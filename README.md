# 🚀 Ultimate Deep Cleaner Pro — Sandy Edition

**Ultimate Deep Cleaner Pro** adalah aplikasi pembersih sistem mendalam (*Deep System & Cache Cleaner*) serta pengoptimal RAM berkecepatan tinggi untuk Windows, menggunakan **Native C#/.NET + Win32 C++ Kernel API (`kernel32.dll`, `psapi.dll`, `shell32.dll`)**.

---

## 👑 Official Owner & Creator
- **Owner**: **Sandy**
- **Instagram**: [@sandy_gimbal](https://www.instagram.com/sandy_gimbal)
- **TikTok**: [@Sandy.gimbal](https://www.tiktok.com/@Sandy.gimbal)

---

## ✨ Fitur Utama

1. **📊 Pemantauan Penyimpanan & RAM Otomatis (Real-Time)**
   - Membaca kapasitas bebas dan terpakai pada **Partisi Sistem C:**, **Partisi D:**, serta **Memori RAM** secara langsung setiap 2 detik melalui Win32 Kernel API (`GlobalMemoryStatusEx`).
2. **🧹 1-Klik Pembersih Mendalam Sampai ke Akar**
   - **Cache Developer**: `.gradle` (caches, daemon, wrapper, native), Android Studio caches/index/log/tmp, `npm-cache`, `pip/uv cache`, Flutter `Pub\Cache`, dan `ms-playwright`.
   - **Sisa Installer & Cache Peta ArcGIS Pro (ESRI)**: File ekstrak installer `.cab`/`.msi` dan cache lokal ESRI.
   - **Cache Browser & Aplikasi**: Google Chrome & Microsoft Edge (semua profil: `Default`, `Profile *`, `Cache`, `Code Cache`, `GPUCache`, `Service Worker`), TikTok LIVE Studio, dan VS Code.
   - **Sampah Sistem Windows**: `%TEMP%`, `C:\Windows\Temp`, `C:\Windows\Prefetch`, `CrashDumps`, `Minidump`, dan Windows Error Reporting (`WER`).
   - **Cache Windows Update & Thumbnail**: `SoftwareDistribution\Download`, `DeliveryOptimization`, dan `thumbcache_*.db`.
   - **Keranjang Sampah (Recycle Bin)**: Mengosongkan Recycle Bin seluruh partisi melalui API `SHEmptyRecycleBinW`.
3. **⚡ Boost & Legakan RAM Instan (C++ API)**
   - Memangkas *Working Set* memori dari proses yang berjalan menggunakan API `EmptyWorkingSet` (`psapi.dll`) tanpa menutup aplikasi.
4. **💤 Pengelola Hibernasi (`hiberfil.sys`)**
   - Menghemat hingga **~3.1 GB** ruang di Drive C: dengan fitur 1-klik matikan/aktifkan hibernasi.
5. **🔎 Pencari File Raksasa (> 100 MB)**
   - Memindai dan menampilkan daftar file berukuran besar di Drive C: untuk dibuka di Explorer atau dihapus langsung.
6. **🎉 Efek Perayaan Visual & Suara Kristal (Synthesized PCM WAV)**
   - Dilengkapi gelombang kejut partikel (*Celebration Shockwave & Particle Burst*), jendela kemenangan beranimasi, serta efek suara lonceng kristal yang disintesis langsung di memori.

---

## 🛠️ Cara Menjalankan

1. **Langsung Jalankan File `.exe`**:
   - Klik dua kali pada **`UltimateCleaner_Pro.exe`** atau **`UltimateCleaner_sandy.exe`**.
2. **Mode Administrator (Disarankan untuk Pembersihan Maksimal)**:
   - Klik dua kali pada **`Jalankan_Pembersih_Admin.bat`** atau klik tombol **Mode Admin** di pojok kanan atas aplikasi.

---

## ⚙️ Cara Compile dari Source Code (`UltimateCleaner_Native.cs`)

Aplikasi ini tidak memerlukan instalasi runtime tambahan karena menggunakan compiler bawaan Windows (.NET Framework 4.x):

```powershell
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:winexe /optimize+ /win32icon:"sandy_icon.ico" /out:"UltimateCleaner_Pro.exe" "UltimateCleaner_Native.cs"
```

---

© **Sandy** (`@sandy_gimbal` | `Sandy.gimbal`) — All Rights Reserved.
