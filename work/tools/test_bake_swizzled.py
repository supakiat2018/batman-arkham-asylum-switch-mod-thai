import subprocess
import os

baker = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\bake_switch_fonts.exe"
ttf_bold = r"C:\Users\Supakiat\Downloads\Google_Sans\static\GoogleSans-Bold.ttf"
in_pkg = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\output\Startup_with_f20.xxx"
out_pkg = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\output\test_swizzled_startup_f6.xxx"

cmd = [baker, in_pkg, out_pkg, ttf_bold, "6"]
res = subprocess.run(cmd, capture_output=True, text=True)
print("Baker output:", res.stdout.strip())
if res.returncode != 0:
    print("ERROR:", res.stderr)
else:
    print("Bake successful, file size:", os.path.getsize(out_pkg))
