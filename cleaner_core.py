# -*- coding: utf-8 -*-
"""
Ultimate Deep Cleaner Pro - Core Engine
Handles disk analysis, deep clutter detection, and safe cleanup routines.
"""

import os
import sys
import stat
import glob
import shutil
import ctypes
import subprocess
from pathlib import Path
from typing import Dict, List, Tuple, Callable, Optional


def is_admin() -> bool:
    """Check if the current process has Administrator privileges."""
    try:
        return ctypes.windll.shell32.IsUserAnAdmin() != 0
    except Exception:
        return False


def restart_as_admin():
    """Relaunch the current process with elevated Administrator privileges."""
    script = os.path.abspath(sys.argv[0])
    params = " ".join([f'"{arg}"' for arg in sys.argv[1:]])
    ctypes.windll.shell32.ShellExecuteW(None, "runas", sys.executable, f'"{script}" {params}', None, 1)
    sys.exit(0)


def format_size(bytes_val: float) -> str:
    """Convert bytes into human-readable string (KB, MB, GB)."""
    if bytes_val <= 0:
        return "0 MB"
    for unit in ['B', 'KB', 'MB', 'GB', 'TB']:
        if abs(bytes_val) < 1024.0:
            return f"{bytes_val:3.1f} {unit}"
        bytes_val /= 1024.0
    return f"{bytes_val:.1f} PB"


def get_drive_c_info() -> Dict[str, float]:
    """Get total, used, and free space for drive C in gigabytes."""
    try:
        usage = shutil.disk_usage("C:\\")
        total_gb = usage.total / (1024 ** 3)
        free_gb = usage.free / (1024 ** 3)
        used_gb = usage.used / (1024 ** 3)
        percent_used = (usage.used / usage.total) * 100.0
        return {
            "total_gb": round(total_gb, 2),
            "used_gb": round(used_gb, 2),
            "free_gb": round(free_gb, 2),
            "percent_used": round(percent_used, 1),
            "is_critical": free_gb < 3.0
        }
    except Exception:
        return {
            "total_gb": 0.0,
            "used_gb": 0.0,
            "free_gb": 0.0,
            "percent_used": 0.0,
            "is_critical": False
        }


def remove_readonly(func, path, excinfo):
    """Error handler for shutil.rmtree to clear read-only flag on Windows."""
    try:
        os.chmod(path, stat.S_IWRITE)
        func(path)
    except Exception:
        pass


def safe_delete_file(file_path: str) -> Tuple[bool, int]:
    """Safely delete a file. Returns (success, bytes_freed)."""
    try:
        p = Path(file_path)
        if not p.is_file() and not p.is_symlink():
            return False, 0
        size = p.stat().st_size
        try:
            os.chmod(file_path, stat.S_IWRITE)
        except Exception:
            pass
        os.remove(file_path)
        return True, size
    except Exception:
        return False, 0


def safe_delete_dir_contents(dir_path: str, progress_cb: Optional[Callable[[str, int], None]] = None) -> Tuple[int, int]:
    """
    Safely delete all contents inside dir_path without deleting the root dir.
    Returns (files_deleted, bytes_freed).
    """
    if not os.path.exists(dir_path):
        return 0, 0

    files_deleted = 0
    bytes_freed = 0

    try:
        for entry in os.scandir(dir_path):
            try:
                if entry.is_file() or entry.is_symlink():
                    try:
                        sz = entry.stat().st_size
                    except Exception:
                        sz = 0
                    try:
                        os.chmod(entry.path, stat.S_IWRITE)
                    except Exception:
                        pass
                    try:
                        os.remove(entry.path)
                        files_deleted += 1
                        bytes_freed += sz
                        if progress_cb:
                            progress_cb(entry.path, sz)
                    except Exception:
                        pass
                elif entry.is_dir():
                    # calculate dir size before removal
                    dir_sz = 0
                    dir_files = 0
                    for root, _, files in os.walk(entry.path):
                        for f in files:
                            fp = os.path.join(root, f)
                            try:
                                dir_sz += os.path.getsize(fp)
                                dir_files += 1
                            except Exception:
                                pass
                    try:
                        shutil.rmtree(entry.path, onerror=remove_readonly)
                        files_deleted += dir_files
                        bytes_freed += dir_sz
                        if progress_cb:
                            progress_cb(entry.path, dir_sz)
                    except Exception:
                        # try deleting individual files inside
                        sub_f, sub_b = safe_delete_dir_contents(entry.path, progress_cb)
                        files_deleted += sub_f
                        bytes_freed += sub_b
            except Exception:
                continue
    except Exception:
        pass

    return files_deleted, bytes_freed


def calculate_dir_size(dir_path: str) -> Tuple[int, int]:
    """Calculate total bytes and file count for a directory."""
    if not os.path.exists(dir_path):
        return 0, 0
    total_bytes = 0
    file_count = 0
    try:
        for root, _, files in os.walk(dir_path):
            for f in files:
                fp = os.path.join(root, f)
                try:
                    total_bytes += os.path.getsize(fp)
                    file_count += 1
                except Exception:
                    pass
    except Exception:
        pass
    return file_count, total_bytes


# ---------------------------------------------------------
# Cleaning Target Definitions
# ---------------------------------------------------------

class CleaningCategory:
    def __init__(self, key: str, title: str, description: str, requires_admin: bool = False, is_default: bool = True):
        self.key = key
        self.title = title
        self.description = description
        self.requires_admin = requires_admin
        self.is_default = is_default
        self.file_count = 0
        self.size_bytes = 0
        self.scanned = False

    def scan(self) -> Tuple[int, int]:
        raise NotImplementedError

    def clean(self, progress_cb: Optional[Callable[[str, int], None]] = None) -> Tuple[int, int]:
        raise NotImplementedError


class FolderGroupCategory(CleaningCategory):
    def __init__(self, key: str, title: str, description: str, folder_paths: List[str], requires_admin: bool = False, is_default: bool = True):
        super().__init__(key, title, description, requires_admin, is_default)
        self.folder_paths = folder_paths

    def scan(self) -> Tuple[int, int]:
        total_files = 0
        total_bytes = 0
        for p in self.folder_paths:
            exp_path = os.path.expandvars(p)
            if os.path.exists(exp_path):
                f_count, b_count = calculate_dir_size(exp_path)
                total_files += f_count
                total_bytes += b_count
        self.file_count = total_files
        self.size_bytes = total_bytes
        self.scanned = True
        return total_files, total_bytes

    def clean(self, progress_cb: Optional[Callable[[str, int], None]] = None) -> Tuple[int, int]:
        cleaned_files = 0
        cleaned_bytes = 0
        for p in self.folder_paths:
            exp_path = os.path.expandvars(p)
            if os.path.exists(exp_path):
                f, b = safe_delete_dir_contents(exp_path, progress_cb)
                cleaned_files += f
                cleaned_bytes += b
        self.file_count = 0
        self.size_bytes = 0
        return cleaned_files, cleaned_bytes


class ChromeProfilesCategory(CleaningCategory):
    def __init__(self):
        super().__init__(
            key="browser_chrome",
            title="Google Chrome (Semua Profil Cache)",
            description="Membersihkan cache web, GPU cache, Code cache, dan CacheStorage di semua profil Chrome (Default, Profile 1, 2, dll).",
            requires_admin=False,
            is_default=True
        )

    def _get_cache_dirs(self) -> List[str]:
        user_data = os.path.expandvars(r"%LocalAppData%\Google\Chrome\User Data")
        if not os.path.exists(user_data):
            return []
        cache_dirs = []
        try:
            for item in os.listdir(user_data):
                full_item = os.path.join(user_data, item)
                if os.path.isdir(full_item) and (item == "Default" or item.startswith("Profile ") or item == "System Profile"):
                    for sub in ["Cache", "Code Cache", "GPUCache", os.path.join("Service Worker", "CacheStorage")]:
                        target = os.path.join(full_item, sub)
                        if os.path.exists(target):
                            cache_dirs.append(target)
            # component crx cache
            comp_cache = os.path.join(user_data, "component_crx_cache")
            if os.path.exists(comp_cache):
                cache_dirs.append(comp_cache)
        except Exception:
            pass
        return cache_dirs

    def scan(self) -> Tuple[int, int]:
        total_files = 0
        total_bytes = 0
        for d in self._get_cache_dirs():
            f, b = calculate_dir_size(d)
            total_files += f
            total_bytes += b
        self.file_count = total_files
        self.size_bytes = total_bytes
        self.scanned = True
        return total_files, total_bytes

    def clean(self, progress_cb: Optional[Callable[[str, int], None]] = None) -> Tuple[int, int]:
        cleaned_files = 0
        cleaned_bytes = 0
        for d in self._get_cache_dirs():
            f, b = safe_delete_dir_contents(d, progress_cb)
            cleaned_files += f
            cleaned_bytes += b
        self.file_count = 0
        self.size_bytes = 0
        return cleaned_files, cleaned_bytes


class EdgeProfilesCategory(CleaningCategory):
    def __init__(self):
        super().__init__(
            key="browser_edge",
            title="Microsoft Edge (Semua Profil Cache)",
            description="Membersihkan cache penjelajahan, GPU cache, dan Service Worker cache di semua profil Edge.",
            requires_admin=False,
            is_default=True
        )

    def _get_cache_dirs(self) -> List[str]:
        user_data = os.path.expandvars(r"%LocalAppData%\Microsoft\Edge\User Data")
        if not os.path.exists(user_data):
            return []
        cache_dirs = []
        try:
            for item in os.listdir(user_data):
                full_item = os.path.join(user_data, item)
                if os.path.isdir(full_item) and (item == "Default" or item.startswith("Profile ")):
                    for sub in ["Cache", "Code Cache", "GPUCache", os.path.join("Service Worker", "CacheStorage")]:
                        target = os.path.join(full_item, sub)
                        if os.path.exists(target):
                            cache_dirs.append(target)
            comp_cache = os.path.join(user_data, "component_crx_cache")
            if os.path.exists(comp_cache):
                cache_dirs.append(comp_cache)
        except Exception:
            pass
        return cache_dirs

    def scan(self) -> Tuple[int, int]:
        total_files = 0
        total_bytes = 0
        for d in self._get_cache_dirs():
            f, b = calculate_dir_size(d)
            total_files += f
            total_bytes += b
        self.file_count = total_files
        self.size_bytes = total_bytes
        self.scanned = True
        return total_files, total_bytes

    def clean(self, progress_cb: Optional[Callable[[str, int], None]] = None) -> Tuple[int, int]:
        cleaned_files = 0
        cleaned_bytes = 0
        for d in self._get_cache_dirs():
            f, b = safe_delete_dir_contents(d, progress_cb)
            cleaned_files += f
            cleaned_bytes += b
        self.file_count = 0
        self.size_bytes = 0
        return cleaned_files, cleaned_bytes


class ArcGISInstallerCategory(CleaningCategory):
    def __init__(self):
        super().__init__(
            key="arcgis_installer",
            title="Sisa Setup Installer ArcGIS Pro (Documents)",
            description="Folder ekstrak installer (.cab/.msi) di Documents yang tertinggal setelah instalasi ArcGIS Pro (~3.1 GB). Sangat aman dihapus.",
            requires_admin=False,
            is_default=True
        )

    def _get_target_dirs(self) -> List[str]:
        targets = []
        user_profile = os.environ.get("USERPROFILE", r"C:\Users\Lenovo")
        docs = os.path.join(user_profile, "Documents")
        if os.path.exists(docs):
            for d in os.listdir(docs):
                if "ArcGIS Pro" in d:
                    installer_sub = os.path.join(docs, d, "ArcGISPro")
                    if os.path.exists(installer_sub):
                        targets.append(installer_sub)
                    else:
                        targets.append(os.path.join(docs, d))
        return targets

    def scan(self) -> Tuple[int, int]:
        total_files = 0
        total_bytes = 0
        for d in self._get_target_dirs():
            f, b = calculate_dir_size(d)
            total_files += f
            total_bytes += b
        self.file_count = total_files
        self.size_bytes = total_bytes
        self.scanned = True
        return total_files, total_bytes

    def clean(self, progress_cb: Optional[Callable[[str, int], None]] = None) -> Tuple[int, int]:
        cleaned_files = 0
        cleaned_bytes = 0
        for d in self._get_target_dirs():
            try:
                f, b = calculate_dir_size(d)
                shutil.rmtree(d, onerror=remove_readonly)
                cleaned_files += f
                cleaned_bytes += b
                if progress_cb:
                    progress_cb(d, b)
            except Exception:
                f, b = safe_delete_dir_contents(d, progress_cb)
                cleaned_files += f
                cleaned_bytes += b
        self.file_count = 0
        self.size_bytes = 0
        return cleaned_files, cleaned_bytes


class RecycleBinCategory(CleaningCategory):
    def __init__(self):
        super().__init__(
            key="recycle_bin",
            title="Keranjang Sampah (Recycle Bin)",
            description="Mengosongkan Recycle Bin untuk seluruh drive.",
            requires_admin=False,
            is_default=True
        )

    def scan(self) -> Tuple[int, int]:
        # estimate from C:\$Recycle.Bin
        f_count = 0
        b_count = 0
        rb_path = r"C:\$Recycle.Bin"
        if os.path.exists(rb_path):
            f_count, b_count = calculate_dir_size(rb_path)
        self.file_count = f_count
        self.size_bytes = b_count
        self.scanned = True
        return f_count, b_count

    def clean(self, progress_cb: Optional[Callable[[str, int], None]] = None) -> Tuple[int, int]:
        old_bytes = self.size_bytes
        old_files = self.file_count
        try:
            # SHEmptyRecycleBinW flags: SHERB_NOCONFIRMATION (1) | SHERB_NOPROGRESSUI (2) | SHERB_NOSOUND (4)
            ctypes.windll.shell32.SHEmptyRecycleBinW(None, None, 7)
        except Exception:
            try:
                subprocess.run(
                    ["powershell", "-NoProfile", "-Command", "Clear-RecycleBin -Force -ErrorAction SilentlyContinue"],
                    capture_output=True,
                    timeout=15
                )
            except Exception:
                pass
        if progress_cb:
            progress_cb("Recycle Bin Emptied", old_bytes)
        self.file_count = 0
        self.size_bytes = 0
        return old_files, old_bytes


class ThumbIconCacheCategory(CleaningCategory):
    def __init__(self):
        super().__init__(
            key="thumb_icon_cache",
            title="Thumbnail & Icon Cache Windows",
            description="Database thumbnail gambar/video yang sering menumpuk di Explorer.",
            requires_admin=False,
            is_default=True
        )

    def _get_files(self) -> List[str]:
        files = []
        user_local = os.path.expandvars(r"%LocalAppData%")
        explorer_dir = os.path.join(user_local, r"Microsoft\Windows\Explorer")
        if os.path.exists(explorer_dir):
            for p in glob.glob(os.path.join(explorer_dir, "thumbcache_*.db")):
                files.append(p)
            for p in glob.glob(os.path.join(explorer_dir, "iconcache_*.db")):
                files.append(p)
        legacy_icon = os.path.join(user_local, "IconCache.db")
        if os.path.exists(legacy_icon):
            files.append(legacy_icon)
        return files

    def scan(self) -> Tuple[int, int]:
        files = self._get_files()
        b_sum = 0
        for f in files:
            try:
                b_sum += os.path.getsize(f)
            except Exception:
                pass
        self.file_count = len(files)
        self.size_bytes = b_sum
        self.scanned = True
        return len(files), b_sum

    def clean(self, progress_cb: Optional[Callable[[str, int], None]] = None) -> Tuple[int, int]:
        del_count = 0
        del_bytes = 0
        for f in self._get_files():
            ok, sz = safe_delete_file(f)
            if ok:
                del_count += 1
                del_bytes += sz
                if progress_cb:
                    progress_cb(f, sz)
        self.file_count = 0
        self.size_bytes = 0
        return del_count, del_bytes


# ---------------------------------------------------------
# Category Factory
# ---------------------------------------------------------

def get_all_categories() -> List[CleaningCategory]:
    user_home = os.environ.get("USERPROFILE", r"C:\Users\Lenovo")
    local_app_data = os.environ.get("LOCALAPPDATA", os.path.join(user_home, r"AppData\Local"))
    app_data = os.environ.get("APPDATA", os.path.join(user_home, r"AppData\Roaming"))

    return [
        # --- 1. Sisa File Spesifik & Installer Raksasa ---
        ArcGISInstallerCategory(),

        FolderGroupCategory(
            key="arcgis_cache",
            title="Cache Peta & Raster ArcGIS (ESRI Local Caches)",
            description=r"Cache map tiles, raster preview, dan display cache di AppData\Local\ESRI\Local Caches (~4.5 GB). Aman dibersihkan tanpa mengganggu software.",
            folder_paths=[os.path.join(local_app_data, r"ESRI\Local Caches")],
            requires_admin=False,
            is_default=True
        ),

        # --- 2. Developer & Package Managers (Penyedot Kapasitas Utama) ---
        FolderGroupCategory(
            key="dev_gradle",
            title="Gradle Build & Distribution Caches (.gradle)",
            description=r"Cache compile build, wrapper zip, daemon, dan transform di C:\Users\<User>\.gradle. Sangat aman dihapus jika hanya debugging (~3.5 GB).",
            folder_paths=[
                os.path.join(user_home, r".gradle\wrapper"),
                os.path.join(user_home, r".gradle\caches"),
                os.path.join(user_home, r".gradle\daemon"),
                os.path.join(user_home, r".gradle\.tmp"),
                os.path.join(user_home, r".gradle\native"),
                os.path.join(user_home, r".gradle\workers"),
                os.path.join(user_home, r".gradle\notifications")
            ],
            requires_admin=False,
            is_default=True
        ),

        FolderGroupCategory(
            key="dev_npm",
            title="Node.js NPM Cache",
            description=r"Cache paket node modules yang diunduh di AppData\Local\npm-cache (~2.9 GB).",
            folder_paths=[
                os.path.join(local_app_data, "npm-cache"),
                os.path.join(app_data, "npm-cache")
            ],
            requires_admin=False,
            is_default=True
        ),

        FolderGroupCategory(
            key="dev_python",
            title="Python PIP & UV Cache",
            description=r"Cache wheel file dan installer package python di pip\cache dan uv\cache (~0.7 GB).",
            folder_paths=[
                os.path.join(local_app_data, r"pip\cache"),
                os.path.join(local_app_data, r"uv\cache")
            ],
            requires_admin=False,
            is_default=True
        ),

        FolderGroupCategory(
            key="dev_pub",
            title="Dart & Flutter Pub Cache",
            description=r"Cache paket Flutter/Dart di AppData\Local\Pub\Cache (~0.7 GB).",
            folder_paths=[
                os.path.join(local_app_data, r"Pub\Cache")
            ],
            requires_admin=False,
            is_default=True
        ),

        FolderGroupCategory(
            key="dev_playwright",
            title="Playwright Browser Binaries",
            description=r"File binary headless browser yang diunduh otomatis di AppData\Local\ms-playwright (~0.7 GB).",
            folder_paths=[
                os.path.join(local_app_data, "ms-playwright")
            ],
            requires_admin=False,
            is_default=True
        ),

        # --- 3. Browser Caches ---
        ChromeProfilesCategory(),
        EdgeProfilesCategory(),

        FolderGroupCategory(
            key="tiktok_cache",
            title="TikTok LIVE Studio Gecko Cache",
            description=r"File cache rendering CEF/Gecko di AppData\Roaming\TikTok LIVE Studio\gecko_cache (~0.4 GB).",
            folder_paths=[
                os.path.join(app_data, r"TikTok LIVE Studio\gecko_cache")
            ],
            requires_admin=False,
            is_default=True
        ),

        # --- 4. Cache Sistem Windows & Temp ---
        FolderGroupCategory(
            key="system_user_temp",
            title="File Sementara (User & System Temp)",
            description=r"File sementara aplikasi dan Windows di %TEMP% dan C:\Windows\Temp.",
            folder_paths=[
                os.environ.get("TEMP", os.path.join(local_app_data, "Temp")),
                r"C:\Windows\Temp"
            ],
            requires_admin=False,
            is_default=True
        ),

        FolderGroupCategory(
            key="prefetch",
            title="Windows Prefetch Files",
            description=r"Cache riwayat peluncuran aplikasi Windows di C:\Windows\Prefetch (Memerlukan Admin).",
            folder_paths=[r"C:\Windows\Prefetch"],
            requires_admin=True,
            is_default=True
        ),

        FolderGroupCategory(
            key="dumps_logs",
            title="Crash Dumps, WER & Error Reports",
            description=r"File dump crash aplikasi dan laporan error di CrashDumps, WER, dan CBS logs.",
            folder_paths=[
                os.path.join(local_app_data, "CrashDumps"),
                r"C:\ProgramData\Microsoft\Windows\WER",
                r"C:\Windows\Minidump"
            ],
            requires_admin=False,
            is_default=True
        ),

        ThumbIconCacheCategory(),

        RecycleBinCategory(),

        # --- 5. Windows Update & Delivery Optimization ---
        FolderGroupCategory(
            key="windows_update",
            title="Windows Update Download Cache & Delivery Optimization",
            description=r"File unduhan Windows Update lama di SoftwareDistribution\Download & DeliveryOptimization.",
            folder_paths=[
                r"C:\Windows\SoftwareDistribution\Download",
                r"C:\Windows\SoftwareDistribution\DeliveryOptimization"
            ],
            requires_admin=True,
            is_default=True
        )
    ]


# ---------------------------------------------------------
# Root / Deep OS Optimization Tools (Admin)
# ---------------------------------------------------------

def get_hibernation_status() -> Dict[str, any]:
    """Check if hiberfil.sys exists on C: and its size."""
    h_file = r"C:\hiberfil.sys"
    exists = os.path.exists(h_file)
    sz_gb = 0.0
    if exists:
        try:
            sz_gb = os.path.getsize(h_file) / (1024 ** 3)
        except Exception:
            pass
    return {
        "exists": exists,
        "size_gb": round(sz_gb, 2)
    }


def set_hibernation(mode: str) -> Tuple[bool, str]:
    """
    Control Windows hibernation:
    mode: 'off' (reclaim full size), 'reduced' (50% size), 'on'
    """
    if not is_admin():
        return False, "Diperlukan Hak Akses Administrator!"
    try:
        if mode == "off":
            res = subprocess.run(["powercfg", "/h", "off"], capture_output=True, text=True, timeout=10)
            return (res.returncode == 0, res.stdout or res.stderr or "Hibernasi berhasil dinonaktifkan.")
        elif mode == "reduced":
            res = subprocess.run(["powercfg", "/h", "/type", "reduced"], capture_output=True, text=True, timeout=10)
            return (res.returncode == 0, res.stdout or res.stderr or "Ukuran hibernasi berhasil dikurangi 50%.")
        elif mode == "on":
            res = subprocess.run(["powercfg", "/h", "on"], capture_output=True, text=True, timeout=10)
            return (res.returncode == 0, res.stdout or res.stderr or "Hibernasi berhasil diaktifkan kembali.")
        else:
            return False, "Mode tidak valid."
    except Exception as e:
        return False, str(e)


def run_dism_cleanup(output_cb: Optional[Callable[[str], None]] = None) -> bool:
    """Run DISM Component Store cleanup to purge superseded Windows update backups."""
    if not is_admin():
        if output_cb:
            output_cb("ERROR: Pembersihan WinSxS memerlukan hak Administrator!")
        return False

    cmd = ["dism.exe", "/Online", "/Cleanup-Image", "/StartComponentCleanup", "/ResetBase"]
    try:
        if output_cb:
            output_cb("Memulai pembersihan komponen Windows (DISM WinSxS)...")
            output_cb("Perintah: " + " ".join(cmd))
            output_cb("Proses ini membutuhkan waktu 1-3 menit, harap tunggu...\n")

        proc = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, bufsize=1)
        for line in proc.stdout:
            if output_cb and line.strip():
                output_cb(line.strip())
        proc.wait()

        success = proc.returncode == 0
        if output_cb:
            if success:
                output_cb("\n[SUKSES] Pembersihan komponen WinSxS selesai! File backup update usang telah dihapus.")
            else:
                output_cb(f"\n[SELESAI DENGAN KODE {proc.returncode}] Silakan periksa log.")
        return success
    except Exception as e:
        if output_cb:
            output_cb(f"Error menjalankan DISM: {e}")
        return False


def flush_dns_cache() -> Tuple[bool, str]:
    """Flush DNS resolver cache."""
    try:
        res = subprocess.run(["ipconfig", "/flushdns"], capture_output=True, text=True, timeout=10)
        return (res.returncode == 0, res.stdout.strip())
    except Exception as e:
        return False, str(e)


# ---------------------------------------------------------
# Large File Scanner (> 100 MB)
# ---------------------------------------------------------

def scan_large_files(root_dir: str, min_size_mb: int = 100, limit: int = 50, progress_cb: Optional[Callable[[str], None]] = None) -> List[Dict[str, any]]:
    """Scan root_dir for files exceeding min_size_mb."""
    results = []
    min_bytes = min_size_mb * 1024 * 1024
    count = 0

    try:
        for root, dirs, files in os.walk(root_dir):
            # Skip hidden Windows internal directories
            if any(skip in root for skip in ["$Recycle.Bin", "System Volume Information", "Windows\\WinSxS", "Windows\\System32"]):
                dirs.clear()
                continue

            for f in files:
                count += 1
                if count % 200 == 0 and progress_cb:
                    progress_cb(f"Memindai: {f}")

                fp = os.path.join(root, f)
                try:
                    sz = os.path.getsize(fp)
                    if sz >= min_bytes:
                        mtime = os.path.getmtime(fp)
                        import datetime
                        mtime_str = datetime.datetime.fromtimestamp(mtime).strftime("%Y-%m-%d %H:%M")
                        results.append({
                            "name": f,
                            "path": fp,
                            "size_bytes": sz,
                            "size_str": format_size(sz),
                            "modified": mtime_str
                        })
                except Exception:
                    continue
    except Exception:
        pass

    results.sort(key=lambda x: x["size_bytes"], reverse=True)
    return results[:limit]

