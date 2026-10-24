import os, subprocess, shutil

baker = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\bake_switch_fonts.exe"
ttf = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\lora-bold.ttf"

# We must ensure Export 22 (Flash vector fonts) is preserved.
# Startup_with_f20.xxx has Export 22 already baked with NotoSansThai vector fonts!
cur_startup = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\output\Startup_with_f20.xxx"

startup_exports = [4, 5, 6, 7, 8, 9, 20, 21]

print("=== Baking Startup.xxx fonts ===")
for exp in startup_exports:
    nxt = f"C:\\Users\Supakiat\\Downloads\\Compressed\\Batman Arkham Asylum\\work\\output\\baked_startup_f{exp}.xxx"
    res = subprocess.run([baker, cur_startup, nxt, ttf, str(exp)], capture_output=True, text=True)
    print(f"Startup Exp {exp}:", res.stdout.strip())
    if res.returncode != 0:
        print("Error:", res.stderr)
        raise RuntimeError(f"Failed to bake Startup exp {exp}")
    cur_startup = nxt

final_startup = cur_startup

print("\n=== Baking CommonGame_LOC_INT.xxx fonts ===")
cur_cg = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\decompressed\CommonGame_LOC_INT.xxx"
cg_exports = [1, 2, 3, 4]
for exp in cg_exports:
    nxt = f"C:\\Users\Supakiat\\Downloads\\Compressed\\Batman Arkham Asylum\\work\\output\\baked_cg_f{exp}.xxx"
    res = subprocess.run([baker, cur_cg, nxt, ttf, str(exp)], capture_output=True, text=True)
    print(f"CommonGame Exp {exp}:", res.stdout.strip())
    if res.returncode != 0:
        print("Error:", res.stderr)
        raise RuntimeError(f"Failed to bake CommonGame exp {exp}")
    cur_cg = nxt

final_cg = cur_cg

print("\nBaking complete!")
print("Final Startup:", final_startup, os.path.getsize(final_startup), "bytes")
print("Final CommonGame:", final_cg, os.path.getsize(final_cg), "bytes")

# Verify Export 22 in final_startup
with open(final_startup, "rb") as f:
    d = f.read()
for name in [b"Bou Collegiate", b"Vox", b"Rockwell WGL", b"Noto Sans"]:
    print(f"Vector font {name.decode()}:", name in d)

# Deploy to Mod and Yuzu
targets = [
    r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\modthai\0100E870163CA000\romfs\BmGame\CookedSwitch",
    r"C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame\CookedSwitch"
]

for t in targets:
    shutil.copy2(final_startup, os.path.join(t, "Startup.xxx"))
    shutil.copy2(final_cg, os.path.join(t, "CommonGame_LOC_INT.xxx"))
    print("Deployed to:", t)
