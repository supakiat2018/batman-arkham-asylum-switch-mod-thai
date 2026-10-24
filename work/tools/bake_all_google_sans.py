import os
import subprocess
import shutil

baker = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\bake_switch_fonts.exe"
ttf_bold = r"C:\Users\Supakiat\Downloads\Google_Sans\static\GoogleSans-Bold.ttf"

# Target directories
mod_dir = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\modthai\0100E870163CA000\romfs\BmGame\CookedSwitch"
yuzu_dir = r"C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame\CookedSwitch"
out_dir = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\output"

# 1. Startup.xxx
# Base with pristine Export 22 Flash vector fonts
cur_startup = os.path.join(out_dir, "Startup_with_f20.xxx")
startup_exports = [4, 5, 6, 7, 8, 9, 20, 21]

print("========================================")
print("=== Baking Google Sans into Startup.xxx ===")
print("========================================")

for exp in startup_exports:
    nxt = os.path.join(out_dir, f"gs_startup_f{exp}.xxx")
    cmd = [baker, cur_startup, nxt, ttf_bold, str(exp)]
    res = subprocess.run(cmd, capture_output=True, text=True)
    print(f"Startup Exp {exp:2d}: {res.stdout.strip()}")
    if res.returncode != 0:
        print("ERROR:", res.stderr)
        raise RuntimeError(f"Failed baking Startup Exp {exp}")
    cur_startup = nxt

final_startup = cur_startup

# Verify Export 22 in final Startup
with open(final_startup, "rb") as f:
    d = f.read()
print("\n--- Verifying Export 22 (Flash Vector Fonts) ---")
for name in [b"Bou Collegiate", b"Vox", b"Rockwell WGL", b"Noto Sans"]:
    found = name in d
    print(f"  Vector font '{name.decode()}': {found}")
    if not found:
        raise RuntimeError(f"Export 22 font {name} missing!")

print("\n================================================")
print("=== Baking Google Sans into CommonGame_LOC_INT.xxx ===")
print("================================================")

cur_cg = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\decompressed\CommonGame_LOC_INT.xxx"
cg_exports = [1, 2, 3, 4]

for exp in cg_exports:
    nxt = os.path.join(out_dir, f"gs_cg_f{exp}.xxx")
    cmd = [baker, cur_cg, nxt, ttf_bold, str(exp)]
    res = subprocess.run(cmd, capture_output=True, text=True)
    print(f"CommonGame Exp {exp:2d}: {res.stdout.strip()}")
    if res.returncode != 0:
        print("ERROR:", res.stderr)
        raise RuntimeError(f"Failed baking CommonGame Exp {exp}")
    cur_cg = nxt

final_cg = cur_cg

print("\n========================================")
print("=== Deploying to Mod & Yuzu Directories ===")
print("========================================")

for dest in [mod_dir, yuzu_dir]:
    shutil.copy2(final_startup, os.path.join(dest, "Startup.xxx"))
    shutil.copy2(final_cg, os.path.join(dest, "CommonGame_LOC_INT.xxx"))
    print(f"Deployed to: {dest}")
    print(f"  Startup.xxx: {os.path.getsize(os.path.join(dest, 'Startup.xxx')):,} bytes")
    print(f"  CommonGame_LOC_INT.xxx: {os.path.getsize(os.path.join(dest, 'CommonGame_LOC_INT.xxx')):,} bytes")

print("\nAll baking completed successfully!")
