import os

orig_toc = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\SwitchTOC.txt"
mod_base = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\modthai\0100E870163CA000\romfs\BmGame"
yuzu_base = r"C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame"

with open(orig_toc, "r", encoding="utf-8") as f:
    lines = [line.strip() for line in f if line.strip()]

updated_lines = []
modified_count = 0

for line in lines:
    parts = line.split(" ")
    if len(parts) >= 5:
        rel_path = parts[3].replace("..\\BmGame\\", "")
        disk_path = os.path.join(mod_base, rel_path)
        if rel_path != "SwitchTOC.txt" and os.path.exists(disk_path):
            actual_sz = os.path.getsize(disk_path)
            if actual_sz != int(parts[0]):
                modified_count += 1
            parts[0] = str(actual_sz)
        updated_lines.append(parts)

# Now solve self-referential SwitchTOC.txt size
# We iterate until size converges
self_size = 175000
for _ in range(10):
    for parts in updated_lines:
        if parts[3] == "..\\BmGame\\SwitchTOC.txt":
            parts[0] = str(self_size)
    text = "\r\n".join(" ".join(p) for p in updated_lines) + "\r\n"
    actual_len = len(text.encode("utf-8"))
    if actual_len == self_size:
        break
    self_size = actual_len

print(f"Updated SwitchTOC.txt: {modified_count} file sizes updated to match modthai disk files.")
print(f"Self-size stabilized at {self_size} bytes.")

mod_toc = os.path.join(mod_base, "SwitchTOC.txt")
yuzu_toc = os.path.join(yuzu_base, "SwitchTOC.txt")

with open(mod_toc, "wb") as f:
    f.write(text.encode("utf-8"))

with open(yuzu_toc, "wb") as f:
    f.write(text.encode("utf-8"))

print(f"Saved to {mod_toc}")
print(f"Saved to {yuzu_toc}")
