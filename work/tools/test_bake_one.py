import subprocess
import os

baker = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\bake_switch_fonts.exe"
ttf = r"C:\Users\Supakiat\Downloads\Google_Sans\static\GoogleSans-Bold.ttf"
in_pkg = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\output\Startup_with_f20.xxx"
out_pkg = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\output\test_gs_startup_f6.xxx"

res = subprocess.run([baker, in_pkg, out_pkg, ttf, "6"], capture_output=True, text=True)
print("Baker output:", res.stdout.strip())
if res.returncode != 0:
    print("Baker error:", res.stderr)
else:
    print("Success! Output size:", os.path.getsize(out_pkg))
