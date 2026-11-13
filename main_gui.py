# -*- coding: utf-8 -*-
"""
Ultimate Deep Cleaner Pro (Sandy Edition)
A modern Windows Deep Cleaning application with CustomTkinter.
Scans and cleans hidden caches down to system roots with 1-click execution.
"""

import os
import sys
import time
import shutil
import threading
import subprocess
import tkinter as tk
from tkinter import messagebox, filedialog
import customtkinter as ctk

# Import core engine
import cleaner_core


# Configure CustomTkinter theme
ctk.set_appearance_mode("Dark")
ctk.set_default_color_theme("blue")


class UltimateCleanerApp(ctk.CTk):
    def __init__(self):
        super().__init__()

        self.title("Ultimate Deep Cleaner Pro (Sandy Edition) - Pembersih Akar Windows")
        self.geometry("1020x750")
        self.minsize(920, 650)

        # Center window on screen
        self._center_window()

        self.is_elevated = cleaner_core.is_admin()
        self.categories = cleaner_core.get_all_categories()
        self.cat_vars = {}
        self.cat_cards = {}
        self.is_busy = False

        self._build_ui()
        self._refresh_drive_info()

        # Trigger initial fast scan in background
        self.after(500, self._start_scan_thread)

    def _center_window(self):
        self.update_idletasks()
        w = 1020
        h = 750
        sw = self.winfo_screenwidth()
        sh = self.winfo_screenheight()
        x = (sw - w) // 2
        y = (sh - h) // 2
        self.geometry(f"{w}x{h}+{x}+{y}")

    def _build_ui(self):
        # Main grid configuration
        self.grid_rowconfigure(1, weight=1)
        self.grid_columnconfigure(0, weight=1)

        # ---------------- Top Header ----------------
        header_frame = ctk.CTkFrame(self, corner_radius=0, fg_color=("#1e293b", "#0f172a"))
        header_frame.grid(row=0, column=0, sticky="ew", padx=0, pady=0)
        header_frame.grid_columnconfigure(0, weight=1)

        header_content = ctk.CTkFrame(header_frame, fg_color="transparent")
        header_content.pack(fill="x", padx=20, pady=12)

        # Left: Titles
        title_box = ctk.CTkFrame(header_content, fg_color="transparent")
        title_box.pack(side="left", fill="y")

        lbl_app = ctk.CTkLabel(
            title_box,
            text="⚡ Ultimate Deep Cleaner Pro",
            font=ctk.CTkFont(size=22, weight="bold"),
            text_color="#38bdf8"
        )
        lbl_app.pack(anchor="w")

        lbl_sub = ctk.CTkLabel(
            title_box,
            text="Pembersih Mendalam Windows & Cache Hingga ke Akar Sistem • Sandy Edition",
            font=ctk.CTkFont(size=12),
            text_color="#94a3b8"
        )
        lbl_sub.pack(anchor="w")

        # Right: Admin Pill & Theme
        right_box = ctk.CTkFrame(header_content, fg_color="transparent")
        right_box.pack(side="right", fill="y")

        if self.is_elevated:
            admin_badge = ctk.CTkLabel(
                right_box,
                text="🛡️ ADMINISTRATOR: AKTIF",
                font=ctk.CTkFont(size=12, weight="bold"),
                text_color="#22c55e",
                fg_color="#14532d",
                corner_radius=6,
                padx=12,
                pady=5
            )
            admin_badge.pack(side="left", padx=8)
        else:
            admin_btn = ctk.CTkButton(
                right_box,
                text="⚠️ Mode Terbatas • Klik Buka Sebagai Admin",
                font=ctk.CTkFont(size=12, weight="bold"),
                fg_color="#b45309",
                hover_color="#d97706",
                text_color="#ffffff",
                corner_radius=6,
                command=self._elevate_admin
            )
            admin_btn.pack(side="left", padx=8)

        # ---------------- Drive C Overview Card ----------------
        self.gauge_frame = ctk.CTkFrame(self, fg_color=("#334155", "#1e293b"), corner_radius=12)
        self.gauge_frame.grid(row=1, column=0, sticky="nsew", padx=16, pady=(10, 5))
        self.gauge_frame.grid_rowconfigure(2, weight=1)
        self.gauge_frame.grid_columnconfigure(0, weight=1)

        # Gauge Top row (Stats & Disk Bar)
        disk_top = ctk.CTkFrame(self.gauge_frame, fg_color="transparent")
        disk_top.grid(row=0, column=0, sticky="ew", padx=16, pady=(12, 4))
        disk_top.grid_columnconfigure(1, weight=1)

        self.lbl_disk_title = ctk.CTkLabel(
            disk_top,
            text="Status Partisi C: (Sistem Windows)",
            font=ctk.CTkFont(size=14, weight="bold"),
            text_color="#f1f5f9"
        )
        self.lbl_disk_title.grid(row=0, column=0, sticky="w")

        self.lbl_disk_numbers = ctk.CTkLabel(
            disk_top,
            text="Memuat status drive...",
            font=ctk.CTkFont(size=13, weight="bold"),
            text_color="#38bdf8"
        )
        self.lbl_disk_numbers.grid(row=0, column=1, sticky="e")

        self.disk_bar = ctk.CTkProgressBar(self.gauge_frame, height=14, corner_radius=7)
        self.disk_bar.grid(row=1, column=0, sticky="ew", padx=16, pady=(2, 10))
        self.disk_bar.set(0.0)

        # ---------------- Tab Navigation ----------------
        self.tabview = ctk.CTkTabview(self.gauge_frame, corner_radius=10)
        self.tabview.grid(row=2, column=0, sticky="nsew", padx=12, pady=(0, 10))

        self.tab_clean = self.tabview.add("⚡ 1-Klik Pembersihan Mendalam")
        self.tab_root = self.tabview.add("🚀 Optimasi Akar Sistem (Admin)")
        self.tab_large = self.tabview.add("🔎 Pencari File Raksasa (>100MB)")
        self.tab_about = self.tabview.add("ℹ️ Analisis & Bantuan")

        self._build_tab_clean()
        self._build_tab_root()
        self._build_tab_large()
        self._build_tab_about()

        # ---------------- Bottom Status Bar ----------------
        bottom_frame = ctk.CTkFrame(self, height=36, corner_radius=0, fg_color=("#1e293b", "#0f172a"))
        bottom_frame.grid(row=2, column=0, sticky="ew")

        self.lbl_status = ctk.CTkLabel(
            bottom_frame,
            text="Siap membersihkan. Klik 'Pindai Sampah' atau langsung '1-Klik Bersihkan'.",
            font=ctk.CTkFont(size=12),
            text_color="#94a3b8"
        )
        self.lbl_status.pack(side="left", padx=16, pady=6)

        self.lbl_freed_total = ctk.CTkLabel(
            bottom_frame,
            text="Total Dibersihkan Sesi Ini: 0 MB",
            font=ctk.CTkFont(size=12, weight="bold"),
            text_color="#22c55e"
        )
        self.lbl_freed_total.pack(side="right", padx=16, pady=6)

        self.session_freed_bytes = 0

    def _elevate_admin(self):
        if messagebox.askyesno(
            "Konfirmasi Mode Administrator",
            "Aplikasi akan dimulai ulang dengan hak akses Administrator (UAC).\n\n"
            "Mode Administrator diperlukan untuk:\n"
            "• Menghapus cache Windows Update & SoftwareDistribution\n"
            "• Mengatur Hibernasi (hiberfil.sys - 3.1 GB)\n"
            "• Membersihkan komponen usang WinSxS (DISM)\n"
            "• Menghapus Windows Prefetch & Crash Dumps sistem\n\n"
            "Lanjutkan restart sebagai Administrator?"
        ):
            cleaner_core.restart_as_admin()

    def _refresh_drive_info(self):
        info = cleaner_core.get_drive_c_info()
        used = info["used_gb"]
        total = info["total_gb"]
        free = info["free_gb"]
        pct = info["percent_used"]

        # Color coding for bar
        if info["is_critical"]:
            self.disk_bar.configure(progress_color="#ef4444")
            status_tag = "⚠️ KRITIS! TERSISA SANGAT SEDIKIT"
        elif pct > 85:
            self.disk_bar.configure(progress_color="#f59e0b")
            status_tag = "Penuh"
        else:
            self.disk_bar.configure(progress_color="#10b981")
            status_tag = "Optimal"

        self.disk_bar.set(pct / 100.0)
        self.lbl_disk_numbers.configure(
            text=f"Terpakai: {used} GB / {total} GB ({pct}%) • Bebas: {free} GB  [{status_tag}]"
        )

    # -------------------------------------------------------------
    # TAB 1: 1-Klik Pembersihan Mendalam
    # -------------------------------------------------------------
    def _build_tab_clean(self):
        tab = self.tab_clean
        tab.grid_rowconfigure(1, weight=1)
        tab.grid_columnconfigure(0, weight=1)

        # Control Action Bar
        action_bar = ctk.CTkFrame(tab, fg_color="transparent")
        action_bar.grid(row=0, column=0, sticky="ew", padx=10, pady=(6, 10))

        self.btn_scan = ctk.CTkButton(
            action_bar,
            text="🔍 Pindai Sampah (Deep Scan)",
            font=ctk.CTkFont(size=13, weight="bold"),
            fg_color="#2563eb",
            hover_color="#1d4ed8",
            width=180,
            height=36,
            command=self._start_scan_thread
        )
        self.btn_scan.pack(side="left", padx=(0, 10))

        btn_select_all = ctk.CTkButton(
            action_bar,
            text="Pilih Semua",
            font=ctk.CTkFont(size=12),
            fg_color="#475569",
            hover_color="#334155",
            width=90,
            height=36,
            command=self._select_all_categories
        )
        btn_select_all.pack(side="left", padx=4)

        btn_unselect = ctk.CTkButton(
            action_bar,
            text="Hapus Pilihan",
            font=ctk.CTkFont(size=12),
            fg_color="#475569",
            hover_color="#334155",
            width=100,
            height=36,
            command=self._unselect_all_categories
        )
        btn_unselect.pack(side="left", padx=4)

        # Huge Clean Button
        self.btn_clean_all = ctk.CTkButton(
            action_bar,
            text="⚡ 1-KLIK BERSIHKAN SEMUA",
            font=ctk.CTkFont(size=14, weight="bold"),
            fg_color="#059669",
            hover_color="#10b981",
            width=230,
            height=36,
            command=self._start_clean_thread
        )
        self.btn_clean_all.pack(side="right", padx=(10, 0))

        # Main Split Content: Left = Categories List, Right = Activity Log
        split_frame = ctk.CTkFrame(tab, fg_color="transparent")
        split_frame.grid(row=1, column=0, sticky="nsew", padx=6, pady=0)
        split_frame.grid_rowconfigure(0, weight=1)
        split_frame.grid_columnconfigure(0, weight=6)
        split_frame.grid_columnconfigure(1, weight=4)

        # Left: Scrollable Categories
        self.scroll_cats = ctk.CTkScrollableFrame(
            split_frame,
            fg_color=("#1e293b", "#0f172a"),
            corner_radius=8,
            label_text="Daftar Target Pembersihan Mendalam"
        )
        self.scroll_cats.grid(row=0, column=0, sticky="nsew", padx=(0, 6), pady=0)
        self.scroll_cats.grid_columnconfigure(0, weight=1)

        self._populate_category_cards()

        # Right: Activity Log & Progress
        log_panel = ctk.CTkFrame(split_frame, fg_color=("#1e293b", "#0f172a"), corner_radius=8)
        log_panel.grid(row=0, column=1, sticky="nsew", padx=(6, 0), pady=0)
        log_panel.grid_rowconfigure(2, weight=1)
        log_panel.grid_columnconfigure(0, weight=1)

        lbl_log_head = ctk.CTkLabel(
            log_panel,
            text="📋 Riwayat & Log Aktivitas",
            font=ctk.CTkFont(size=13, weight="bold"),
            text_color="#f8fafc"
        )
        lbl_log_head.grid(row=0, column=0, sticky="w", padx=12, pady=(10, 4))

        self.clean_progress = ctk.CTkProgressBar(log_panel, height=8, corner_radius=4)
        self.clean_progress.grid(row=1, column=0, sticky="ew", padx=12, pady=(2, 8))
        self.clean_progress.set(0.0)

        self.txt_log = ctk.CTkTextbox(
            log_panel,
            font=ctk.CTkFont(family="Consolas", size=11),
            fg_color=("#0f172a", "#020617"),
            text_color="#cbd5e1",
            wrap="word"
        )
        self.txt_log.grid(row=2, column=0, sticky="nsew", padx=12, pady=(0, 10))
        self._log("Aplikasi siap. Klik 'Pindai Sampah (Deep Scan)' untuk melihat ukuran terkini.")

    def _populate_category_cards(self):
        for widget in self.scroll_cats.winfo_children():
            widget.destroy()

        self.cat_cards.clear()
        self.cat_vars.clear()

        for idx, cat in enumerate(self.categories):
            var = ctk.BooleanVar(value=cat.is_default)
            self.cat_vars[cat.key] = var

            card = ctk.CTkFrame(self.scroll_cats, fg_color=("#334155", "#1e293b"), corner_radius=8)
            card.grid(row=idx, column=0, sticky="ew", padx=6, pady=4)
            card.grid_columnconfigure(1, weight=1)

            # Checkbox
            chk = ctk.CTkCheckBox(
                card,
                text="",
                variable=var,
                width=24,
                checkbox_width=20,
                checkbox_height=20
            )
            chk.grid(row=0, column=0, rowspan=2, padx=(10, 6), pady=8)

            # Title
            admin_tag = " [Admin]" if cat.requires_admin and not self.is_elevated else ""
            lbl_title = ctk.CTkLabel(
                card,
                text=f"{cat.title}{admin_tag}",
                font=ctk.CTkFont(size=13, weight="bold"),
                text_color="#f8fafc",
                anchor="w"
            )
            lbl_title.grid(row=0, column=1, sticky="w", padx=0, pady=(6, 0))

            # Size badge
            lbl_size = ctk.CTkLabel(
                card,
                text="Menghitung...",
                font=ctk.CTkFont(size=12, weight="bold"),
                text_color="#38bdf8",
                anchor="e"
            )
            lbl_size.grid(row=0, column=2, sticky="e", padx=12, pady=(6, 0))

            # Description
            lbl_desc = ctk.CTkLabel(
                card,
                text=cat.description,
                font=ctk.CTkFont(size=11),
                text_color="#94a3b8",
                anchor="w",
                justify="left",
                wraplength=420
            )
            lbl_desc.grid(row=1, column=1, columnspan=2, sticky="w", padx=0, pady=(0, 6))

            self.cat_cards[cat.key] = {
                "frame": card,
                "size_label": lbl_size,
                "checkbox": chk,
                "category": cat
            }

    def _select_all_categories(self):
        for var in self.cat_vars.values():
            var.set(True)

    def _unselect_all_categories(self):
        for var in self.cat_vars.values():
            var.set(False)

    def _log(self, text: str):
        self.txt_log.insert("end", text + "\n")
        self.txt_log.see("end")

    # -------------------------------------------------------------
    # Scanning Logic (Threaded)
    # -------------------------------------------------------------
    def _start_scan_thread(self):
        if self.is_busy:
            return
        self.is_busy = True
        self.btn_scan.configure(state="disabled", text="⏳ Memindai...")
        self.btn_clean_all.configure(state="disabled")
        self.clean_progress.set(0.0)
        self.clean_progress.configure(mode="indeterminate")
        self.clean_progress.start()
        self.lbl_status.configure(text="Sedang memindai file sampah dan cache mendalam...")

        threading.Thread(target=self._run_scan, daemon=True).start()

    def _run_scan(self):
        total_bytes = 0
        total_files = 0

        for cat in self.categories:
            try:
                files, b = cat.scan()
                total_files += files
                total_bytes += b

                # Update UI for this card
                sz_str = cleaner_core.format_size(b)
                self.after(0, self._update_cat_size_ui, cat.key, sz_str, files)
            except Exception as e:
                self.after(0, self._log, f"Gagal memindai {cat.title}: {e}")

        self.after(0, self._finish_scan, total_bytes, total_files)

    def _update_cat_size_ui(self, key: str, sz_str: str, file_count: int):
        if key in self.cat_cards:
            lbl = self.cat_cards[key]["size_label"]
            if file_count > 0:
                lbl.configure(text=f"{sz_str} ({file_count:,} file)", text_color="#38bdf8")
            else:
                lbl.configure(text="0 MB (Bersih)", text_color="#64748b")

    def _finish_scan(self, total_bytes: int, total_files: int):
        self.clean_progress.stop()
        self.clean_progress.configure(mode="determinate")
        self.clean_progress.set(1.0)
        self.btn_scan.configure(state="normal", text="🔍 Pindai Sampah (Deep Scan)")
        self.btn_clean_all.configure(state="normal")
        self.is_busy = False

        total_sz = cleaner_core.format_size(total_bytes)
        self.lbl_status.configure(
            text=f"Pemindaian selesai: Terdeteksi {total_sz} file sampah dari {total_files:,} item."
        )
        self._log("-" * 45)
        self._log(f"✅ PEMINDAIAN SELESAI: Ditemukan {total_sz} sampah siap dibersihkan!")
        self._log("Pilih kategori yang ingin dibersihkan lalu klik '1-KLIK BERSIHKAN SEMUA'.")
        self._refresh_drive_info()

    # -------------------------------------------------------------
    # Cleaning Logic (Threaded)
    # -------------------------------------------------------------
    def _start_clean_thread(self):
        if self.is_busy:
            return

        selected_cats = [
            cat for cat in self.categories
            if self.cat_vars.get(cat.key, ctk.BooleanVar(value=False)).get()
        ]

        if not selected_cats:
            messagebox.showwarning("Peringatan", "Pilih minimal satu kategori untuk dibersihkan!")
            return

        # Calculate selected size
        sel_bytes = sum(c.size_bytes for c in selected_cats)
        sel_str = cleaner_core.format_size(sel_bytes)

        if not messagebox.askyesno(
            "Konfirmasi Pembersihan Mendalam",
            f"Anda akan membersihkan {len(selected_cats)} kategori terpilih.\n"
            f"Estimasi ruang yang akan dibebaskan: ~{sel_str}\n\n"
            "File sementara, cache aplikasi, dan sisa installer akan dihapus dengan aman.\n"
            "Lanjutkan pembersihan sekarang?"
        ):
            return

        self.is_busy = True
        self.btn_scan.configure(state="disabled")
        self.btn_clean_all.configure(state="disabled", text="🧹 Membersihkan...")
        self.clean_progress.set(0.0)
        self._log("\n" + "=" * 45)
        self._log(f"⚡ MEMULAI PEMBERSIHAN MENDALAM ({len(selected_cats)} kategori)...")

        threading.Thread(target=self._run_clean, args=(selected_cats,), daemon=True).start()

    def _run_clean(self, selected_cats):
        total_freed = 0
        total_files = 0
        total_cats = len(selected_cats)

        for idx, cat in enumerate(selected_cats):
            self.after(0, self.lbl_status.configure, {"text": f"Sedang membersihkan: {cat.title}..."})
            self.after(0, self._log, f"▶ Membersihkan: {cat.title}...")

            def progress_cb(path, size_bytes):
                # Throttle log slightly for UI smoothness
                pass

            try:
                del_files, del_bytes = cat.clean(progress_cb)
                total_files += del_files
                total_freed += del_bytes

                freed_str = cleaner_core.format_size(del_bytes)
                self.after(0, self._log, f"   ✓ Berhasil: {freed_str} dibebaskan ({del_files:,} file)")
                self.after(0, self._update_cat_size_ui, cat.key, "0 MB (Bersih)", 0)
            except Exception as e:
                self.after(0, self._log, f"   ✗ Peringatan: {e}")

            # update progress bar
            pct = (idx + 1) / total_cats
            self.after(0, self.clean_progress.set, pct)

        self.after(0, self._finish_clean, total_freed, total_files)

    def _finish_clean(self, total_freed: int, total_files: int):
        self.session_freed_bytes += total_freed
        freed_str = cleaner_core.format_size(total_freed)
        session_str = cleaner_core.format_size(self.session_freed_bytes)

        self.lbl_freed_total.configure(text=f"Total Dibersihkan Sesi Ini: {session_str}")
        self.lbl_status.configure(
            text=f"Pembersihan selesai! Berhasil membebaskan {freed_str} ruang penyimpanan."
        )

        self._log("-" * 45)
        self._log(f"🎉 SUKSES! Pembersihan selesai.")
        self._log(f"   • Total ruang dibebaskan: {freed_str}")
        self._log(f"   • Total file terhapus: {total_files:,}")
        self._log("=" * 45 + "\n")

        self.btn_scan.configure(state="normal")
        self.btn_clean_all.configure(state="normal", text="⚡ 1-KLIK BERSIHKAN SEMUA")
        self.is_busy = False

        self._refresh_drive_info()
        messagebox.showinfo(
            "Pembersihan Berhasil!",
            f"Pembersihan mendalam telah selesai!\n\n"
            f"Ruang yang berhasil dibebaskan: {freed_str}\n"
            f"Drive C sekarang memiliki lebih banyak ruang bebas."
        )

    # -------------------------------------------------------------
    # TAB 2: Optimasi Akar Sistem (Admin / Hibernasi / DISM)
    # -------------------------------------------------------------
    def _build_tab_root(self):
        tab = self.tab_root
        tab.grid_columnconfigure(0, weight=1)

        # Card 1: Hibernation Manager
        hib_frame = ctk.CTkFrame(tab, fg_color=("#1e293b", "#0f172a"), corner_radius=10)
        hib_frame.pack(fill="x", padx=16, pady=10)
        hib_frame.grid_columnconfigure(1, weight=1)

        lbl_hib_title = ctk.CTkLabel(
            hib_frame,
            text="💤 Manajemen Hibernasi Windows (hiberfil.sys)",
            font=ctk.CTkFont(size=14, weight="bold"),
            text_color="#f8fafc"
        )
        lbl_hib_title.pack(anchor="w", padx=16, pady=(12, 4))

        self.lbl_hib_status = ctk.CTkLabel(
            hib_frame,
            text="Status hiberfil.sys: Memeriksa...",
            font=ctk.CTkFont(size=12),
            text_color="#38bdf8"
        )
        self.lbl_hib_status.pack(anchor="w", padx=16, pady=2)

        lbl_hib_expl = ctk.CTkLabel(
            hib_frame,
            text=(
                "File hiberfil.sys adalah file memori untuk fitur Hibernate dan Fast Startup. "
                "Ukurannya bisa mencapai 3-8 GB langsung di C:\\. Jika Anda tidak pernah memakai mode Hibernate, "
                "Anda bisa menonaktifkannya untuk membebaskan ruang seketika, atau mengompresnya 50%."
            ),
            font=ctk.CTkFont(size=11),
            text_color="#94a3b8",
            wraplength=800,
            justify="left"
        )
        lbl_hib_expl.pack(anchor="w", padx=16, pady=4)

        hib_btns = ctk.CTkFrame(hib_frame, fg_color="transparent")
        hib_btns.pack(fill="x", padx=16, pady=(8, 14))

        btn_hib_off = ctk.CTkButton(
            hib_btns,
            text="🔴 Matikan Hibernasi (Bebaskan ~3.1 GB)",
            font=ctk.CTkFont(size=12, weight="bold"),
            fg_color="#dc2626",
            hover_color="#b91c1c",
            command=lambda: self._handle_hibernation("off")
        )
        btn_hib_off.pack(side="left", padx=(0, 10))

        btn_hib_red = ctk.CTkButton(
            hib_btns,
            text="🟡 Kurangi Ukuran 50% (Reduced)",
            font=ctk.CTkFont(size=12, weight="bold"),
            fg_color="#d97706",
            hover_color="#b45309",
            command=lambda: self._handle_hibernation("reduced")
        )
        btn_hib_red.pack(side="left", padx=10)

        btn_hib_on = ctk.CTkButton(
            hib_btns,
            text="🟢 Aktifkan Hibernasi Normal",
            font=ctk.CTkFont(size=12),
            fg_color="#475569",
            hover_color="#334155",
            command=lambda: self._handle_hibernation("on")
        )
        btn_hib_on.pack(side="left", padx=10)

        # Card 2: DISM Component Store (WinSxS)
        dism_frame = ctk.CTkFrame(tab, fg_color=("#1e293b", "#0f172a"), corner_radius=10)
        dism_frame.pack(fill="both", expand=True, padx=16, pady=10)

        lbl_dism_title = ctk.CTkLabel(
            dism_frame,
            text="🧩 Pembersihan Komponen WinSxS (DISM Component Store)",
            font=ctk.CTkFont(size=14, weight="bold"),
            text_color="#f8fafc"
        )
        lbl_dism_title.pack(anchor="w", padx=16, pady=(12, 4))

        lbl_dism_expl = ctk.CTkLabel(
            dism_frame,
            text=(
                "Folder C:\\Windows\\WinSxS menyimpan file cadangan dari update-update Windows sebelumnya. "
                "DISM ResetBase akan menghapus versi lama yang sudah tergantikan, membebaskan 2-5 GB "
                "kapasitas sistem tanpa merusak Windows."
            ),
            font=ctk.CTkFont(size=11),
            text_color="#94a3b8",
            wraplength=800,
            justify="left"
        )
        lbl_dism_expl.pack(anchor="w", padx=16, pady=4)

        dism_btn_row = ctk.CTkFrame(dism_frame, fg_color="transparent")
        dism_btn_row.pack(fill="x", padx=16, pady=6)

        self.btn_dism = ctk.CTkButton(
            dism_btn_row,
            text="🛠️ Jalankan DISM Cleanup WinSxS",
            font=ctk.CTkFont(size=12, weight="bold"),
            fg_color="#7c3aed",
            hover_color="#6d28d9",
            width=220,
            command=self._start_dism_thread
        )
        self.btn_dism.pack(side="left", padx=(0, 10))

        btn_dns = ctk.CTkButton(
            dism_btn_row,
            text="🌐 Reset DNS Cache (ipconfig /flushdns)",
            font=ctk.CTkFont(size=12),
            fg_color="#475569",
            hover_color="#334155",
            command=self._handle_flush_dns
        )
        btn_dns.pack(side="left", padx=10)

        self.txt_dism_log = ctk.CTkTextbox(
            dism_frame,
            height=140,
            font=ctk.CTkFont(family="Consolas", size=11),
            fg_color=("#0f172a", "#020617"),
            text_color="#cbd5e1"
        )
        self.txt_dism_log.pack(fill="both", expand=True, padx=16, pady=(6, 14))

        self._refresh_hibernation_ui()

    def _refresh_hibernation_ui(self):
        st = cleaner_core.get_hibernation_status()
        if st["exists"]:
            self.lbl_hib_status.configure(
                text=f"Status: Aktif • hiberfil.sys terdeteksi berukuran {st['size_gb']} GB di C:\\",
                text_color="#ef4444"
            )
        else:
            self.lbl_hib_status.configure(
                text="Status: Dinonaktifkan (hiberfil.sys tidak ada / 0 GB - Sangat Hemat Ruang)",
                text_color="#22c55e"
            )

    def _handle_hibernation(self, mode: str):
        if not self.is_elevated:
            messagebox.showwarning(
                "Akses Ditolak",
                "Mengatur hibernasi membutuhkan hak Administrator.\n"
                "Klik tombol 'Buka Sebagai Admin' di pojok kanan atas."
            )
            return

        ok, msg = cleaner_core.set_hibernation(mode)
        if ok:
            messagebox.showinfo("Sukses", f"Perintah hibernasi berhasil diterapkan!\n\n{msg}")
            self._refresh_hibernation_ui()
            self._refresh_drive_info()
        else:
            messagebox.showerror("Gagal", f"Gagal mengubah status hibernasi:\n{msg}")

    def _start_dism_thread(self):
        if not self.is_elevated:
            messagebox.showwarning(
                "Akses Ditolak",
                "Menjalankan DISM membutuhkan hak Administrator.\n"
                "Klik tombol 'Buka Sebagai Admin' di pojok kanan atas."
            )
            return

        if not messagebox.askyesno(
            "Konfirmasi Pembersihan WinSxS",
            "Pembersihan komponen Windows (DISM ResetBase) akan menghapus file rollback update lama.\n"
            "Proses ini berjalan di latar belakang selama 1-3 menit.\n\n"
            "Mulai proses DISM sekarang?"
        ):
            return

        self.btn_dism.configure(state="disabled", text="⏳ Menjalankan DISM...")
        self.txt_dism_log.delete("1.0", "end")

        def run_thread():
            def out_cb(line):
                self.after(0, self._dism_log, line)
            cleaner_core.run_dism_cleanup(out_cb)
            self.after(0, self._finish_dism)

        threading.Thread(target=run_thread, daemon=True).start()

    def _dism_log(self, text: str):
        self.txt_dism_log.insert("end", text + "\n")
        self.txt_dism_log.see("end")

    def _finish_dism(self):
        self.btn_dism.configure(state="normal", text="🛠️ Jalankan DISM Cleanup WinSxS")
        self._refresh_drive_info()
        messagebox.showinfo("DISM Selesai", "Proses pembersihan WinSxS telah selesai.")

    def _handle_flush_dns(self):
        ok, msg = cleaner_core.flush_dns_cache()
        if ok:
            messagebox.showinfo("DNS Cache", "DNS Cache berhasil dibersihkan (Flushed)!")
        else:
            messagebox.showwarning("DNS Cache", f"Hasil: {msg}")

    # -------------------------------------------------------------
    # TAB 3: Pencari File Raksasa (Large Files Finder)
    # -------------------------------------------------------------
    def _build_tab_large(self):
        tab = self.tab_large
        tab.grid_rowconfigure(1, weight=1)
        tab.grid_columnconfigure(0, weight=1)

        # Filter bar
        top_bar = ctk.CTkFrame(tab, fg_color="transparent")
        top_bar.grid(row=0, column=0, sticky="ew", padx=10, pady=8)

        lbl_dir = ctk.CTkLabel(top_bar, text="Folder:", font=ctk.CTkFont(size=12))
        lbl_dir.pack(side="left", padx=(0, 6))

        user_home = os.environ.get("USERPROFILE", r"C:\Users\Lenovo")
        self.entry_large_dir = ctk.CTkEntry(top_bar, width=280)
        self.entry_large_dir.insert(0, user_home)
        self.entry_large_dir.pack(side="left", padx=(0, 8))

        btn_browse = ctk.CTkButton(
            top_bar,
            text="Pilih...",
            width=70,
            fg_color="#475569",
            hover_color="#334155",
            command=self._browse_large_dir
        )
        btn_browse.pack(side="left", padx=(0, 16))

        lbl_size = ctk.CTkLabel(top_bar, text="Minimal:", font=ctk.CTkFont(size=12))
        lbl_size.pack(side="left", padx=(0, 6))

        self.combo_size = ctk.CTkComboBox(
            top_bar,
            values=["100 MB", "250 MB", "500 MB", "1 GB", "2 GB"],
            width=100
        )
        self.combo_size.set("100 MB")
        self.combo_size.pack(side="left", padx=(0, 16))

        self.btn_search_large = ctk.CTkButton(
            top_bar,
            text="🔍 Cari File Besar",
            font=ctk.CTkFont(size=12, weight="bold"),
            fg_color="#2563eb",
            hover_color="#1d4ed8",
            command=self._start_large_search
        )
        self.btn_search_large.pack(side="left", padx=6)

        # Scrollable list for results
        self.scroll_large = ctk.CTkScrollableFrame(
            tab,
            fg_color=("#1e293b", "#0f172a"),
            corner_radius=8,
            label_text="Hasil Temuan File Raksasa (Klik 'Buka Lokasi' atau 'Hapus')"
        )
        self.scroll_large.grid(row=1, column=0, sticky="nsew", padx=10, pady=(0, 10))
        self.scroll_large.grid_columnconfigure(0, weight=1)

    def _browse_large_dir(self):
        folder = filedialog.askdirectory(title="Pilih Folder untuk Dipindai")
        if folder:
            self.entry_large_dir.delete(0, "end")
            self.entry_large_dir.insert(0, folder)

    def _start_large_search(self):
        target_dir = self.entry_large_dir.get().strip()
        if not os.path.exists(target_dir):
            messagebox.showerror("Error", "Folder yang dipilih tidak ditemukan.")
            return

        size_str = self.combo_size.get()
        mb_val = 100
        if "250" in size_str: mb_val = 250
        elif "500" in size_str: mb_val = 500
        elif "1 GB" in size_str: mb_val = 1024
        elif "2 GB" in size_str: mb_val = 2048

        self.btn_search_large.configure(state="disabled", text="⏳ Memindai...")
        for w in self.scroll_large.winfo_children():
            w.destroy()

        def search_thread():
            results = cleaner_core.scan_large_files(target_dir, min_size_mb=mb_val, limit=50)
            self.after(0, self._display_large_results, results)

        threading.Thread(target=search_thread, daemon=True).start()

    def _display_large_results(self, results):
        self.btn_search_large.configure(state="normal", text="🔍 Cari File Besar")

        if not results:
            lbl_empty = ctk.CTkLabel(
                self.scroll_large,
                text="Tidak ditemukan file yang melebihi ukuran filter.",
                font=ctk.CTkFont(size=13),
                text_color="#94a3b8"
            )
            lbl_empty.pack(pady=30)
            return

        for idx, item in enumerate(results):
            row_frame = ctk.CTkFrame(self.scroll_large, fg_color=("#334155", "#1e293b"), corner_radius=6)
            row_frame.grid(row=idx, column=0, sticky="ew", padx=6, pady=4)
            row_frame.grid_columnconfigure(0, weight=1)

            # File info
            lbl_name = ctk.CTkLabel(
                row_frame,
                text=f"📄 {item['name']}",
                font=ctk.CTkFont(size=12, weight="bold"),
                text_color="#f8fafc",
                anchor="w"
            )
            lbl_name.grid(row=0, column=0, sticky="w", padx=10, pady=(6, 2))

            lbl_sub = ctk.CTkLabel(
                row_frame,
                text=f"{item['size_str']} • Dimodifikasi: {item['modified']} • {item['path']}",
                font=ctk.CTkFont(size=10),
                text_color="#94a3b8",
                anchor="w"
            )
            lbl_sub.grid(row=1, column=0, sticky="w", padx=10, pady=(0, 6))

            # Buttons
            btn_box = ctk.CTkFrame(row_frame, fg_color="transparent")
            btn_box.grid(row=0, column=1, rowspan=2, padx=10, pady=4)

            btn_open = ctk.CTkButton(
                btn_box,
                text="📂 Buka Lokasi",
                font=ctk.CTkFont(size=11),
                width=90,
                height=28,
                fg_color="#3b82f6",
                hover_color="#2563eb",
                command=lambda p=item['path']: self._open_file_location(p)
            )
            btn_open.pack(side="left", padx=4)

            btn_del = ctk.CTkButton(
                btn_box,
                text="🗑️ Hapus",
                font=ctk.CTkFont(size=11),
                width=70,
                height=28,
                fg_color="#dc2626",
                hover_color="#b91c1c",
                command=lambda p=item['path'], f=row_frame: self._delete_large_file(p, f)
            )
            btn_del.pack(side="left", padx=4)

    def _open_file_location(self, file_path: str):
        if os.path.exists(file_path):
            subprocess.run(["explorer", f"/select,{os.path.abspath(file_path)}"])
        else:
            messagebox.showwarning("File Tidak Ditemukan", "File ini sudah tidak ada di lokasi.")

    def _delete_large_file(self, file_path: str, row_widget):
        if not os.path.exists(file_path):
            row_widget.destroy()
            return

        if messagebox.askyesno(
            "Konfirmasi Hapus File",
            f"Anda yakin ingin menghapus file ini secara permanen?\n\n"
            f"Nama: {os.path.basename(file_path)}\n"
            f"Lokasi: {file_path}"
        ):
            ok, sz = cleaner_core.safe_delete_file(file_path)
            if ok:
                row_widget.destroy()
                self._refresh_drive_info()
                messagebox.showinfo("Terhapus", f"File berhasil dihapus ({cleaner_core.format_size(sz)} dibebaskan).")
            else:
                messagebox.showerror("Gagal", "Gagal menghapus file. Mungkin file sedang digunakan.")

    # -------------------------------------------------------------
    # TAB 4: Info & Analisis
    # -------------------------------------------------------------
    def _build_tab_about(self):
        tab = self.tab_about

        card = ctk.CTkFrame(tab, fg_color=("#1e293b", "#0f172a"), corner_radius=10)
        card.pack(fill="both", expand=True, padx=16, pady=16)

        text_content = (
            "💡 KENAPA DRIVE C BISA CEPAT PENUH PADAHAL APLIKASI SEDIKIT?\n"
            "----------------------------------------------------------------------------------\n\n"
            "1. Sisa Ekstraksi Installer Aplikasi (Contoh: ArcGIS Pro)\n"
            "   Saat Anda menginstal software besar seperti ArcGIS Pro, file instalasi (.cab dan .msi)\n"
            "   diekstrak ke folder 'Documents\\ArcGIS Pro' (~3.1 GB). Setelah software berhasil diinstal,\n"
            "   file ekstraksi ini tidak lagi terpakai namun tetap tersimpan selamanya jika tidak dibersihkan.\n\n"
            "2. Cache Peta & Data Spasial (ESRI Local Caches)\n"
            "   ArcGIS menyimpan cache tile peta dan citra raster di AppData\\Local\\ESRI\\Local Caches (~4.5 GB).\n"
            "   Aplikasi Ultimate Deep Cleaner Pro dapat membersihkan cache ini secara aman.\n\n"
            "3. Cache Developer & Package Manager (Gradle, NPM, PIP, Flutter)\n"
            "   Jika laptop pernah dipakai untuk kompilasi Android/Flutter/Node.js/Python, tool seperti\n"
            "   Gradle (.gradle/caches ~1.7 - 3.5 GB), NPM (~2.9 GB), dan PIP (~0.7 GB) menumpuk ribuan file cache.\n\n"
            "4. File Hibernasi Windows (hiberfil.sys - 3.1 GB)\n"
            "   Secara default, Windows memesan 3-8 GB di C:\\ untuk fitur hibernate dan fast startup.\n"
            "   Melalui tab 'Optimasi Akar Sistem', Anda bisa mematikan fitur ini untuk membebaskan ruang seketika.\n\n"
            "5. Cache Browser Multi-Profil (Chrome, Edge)\n"
            "   Pembersih biasa sering hanya membersihkan profil 'Default'. Aplikasi ini menyisir seluruh profil\n"
            "   (Profile 1, Profile 2, Profile 4, dll) beserta Code Cache dan Service Worker.\n\n"
            "6. WinSxS (Windows Component Store)\n"
            "   Setiap kali Windows Update mengunduh update baru, versi lama tetap disimpan sebagai backup.\n"
            "   Gunakan fitur DISM di tab Optimasi Akar Sistem untuk membuang update usang."
        )

        txt_info = ctk.CTkTextbox(
            card,
            font=ctk.CTkFont(family="Segoe UI", size=12),
            fg_color="transparent",
            text_color="#cbd5e1"
        )
        txt_info.pack(fill="both", expand=True, padx=20, pady=20)
        txt_info.insert("1.0", text_content)
        txt_info.configure(state="disabled")


if __name__ == "__main__":
    app = UltimateCleanerApp()
    app.mainloop()

