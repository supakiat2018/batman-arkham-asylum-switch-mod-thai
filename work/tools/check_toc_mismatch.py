import os

base_mod = r"C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame"
toc_path = os.path.join(base_mod, "SwitchTOC.txt")

mismatches = []
with open(toc_path, "r", encoding="utf-8") as f:
    for line in f:
        line = line.strip()
        if not line: continue
        parts = line.split(" ")
        if len(parts) >= 5:
            toc_sz = int(parts[0])
            rel_path = parts[3].replace("..\\BmGame\\", "")
            disk_path = os.path.join(base_mod, rel_path)
            if os.path.exists(disk_path):
                disk_sz = os.path.getsize(disk_path)
                if disk_sz != toc_sz:
                    mismatches.append((rel_path, toc_sz, disk_sz))

print(f"Total file size mismatches in SwitchTOC.txt: {len(mismatches)}")
for m in mismatches:
    print(f"  {m[0]}: TOC={m[1]:,}, Disk={m[2]:,}, Delta={m[2] - m[1]:,}")
