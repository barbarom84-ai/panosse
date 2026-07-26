from pathlib import Path
from collections import deque
from PIL import Image

assets = Path(__file__).resolve().parents[1] / "src" / "Panosse.WinUI" / "Assets"
raw = assets / "panosse-raw.png"
src_path = assets / "panosse.png"

img = Image.open(raw if raw.exists() else src_path).convert("RGBA")
pixels = img.load()
w, h = img.size
bg = pixels[0, 0][:3]


def is_canvas(px) -> bool:
    r, g, b = px[0], px[1], px[2]
    # Flat Figma screenshot canvas: near-neutral light gray only.
    if abs(r - g) > 2 or abs(g - b) > 2 or abs(r - b) > 2:
        return False
    if r < 220 or r > 240:
        return False
    return abs(r - bg[0]) <= 4


visited = [[False] * h for _ in range(w)]
q = deque()
for x, y in ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)):
    if is_canvas(pixels[x, y]):
        q.append((x, y))
        visited[x][y] = True

while q:
    x, y = q.popleft()
    pixels[x, y] = (0, 0, 0, 0)
    for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
        if 0 <= nx < w and 0 <= ny < h and not visited[nx][ny] and is_canvas(pixels[nx, ny]):
            visited[nx][ny] = True
            q.append((nx, ny))

img.save(src_path, format="PNG")
print(
    "corner", img.getpixel((0, 0)),
    "center", img.getpixel((256, 256)),
    "mop", img.getpixel((256, 300)),
    "handle", img.getpixel((256, 80)),
)

master = img.copy()


def fit_square(size: int) -> Image.Image:
    return master.resize((size, size), Image.Resampling.LANCZOS)


def save_png(path: Path, image: Image.Image) -> None:
    image.save(path, format="PNG")
    print(f"wrote {path.name} {image.size}")


ico_sizes = [(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
fit_square(256).save(assets / "panosse.ico", format="ICO", sizes=ico_sizes)
print(f"wrote panosse.ico bytes={(assets / 'panosse.ico').stat().st_size}")

save_png(assets / "StoreLogo.png", fit_square(50))
save_png(assets / "Square44x44Logo.scale-200.png", fit_square(88))
save_png(assets / "Square44x44Logo.targetsize-24_altform-unplated.png", fit_square(24))
save_png(assets / "Square150x150Logo.scale-200.png", fit_square(300))
save_png(assets / "LockScreenLogo.scale-200.png", fit_square(96))


def place_on_canvas(width: int, height: int, icon_size: int) -> Image.Image:
    canvas = Image.new("RGBA", (width, height), (33, 150, 243, 255))
    icon = fit_square(icon_size)
    canvas.alpha_composite(icon, ((width - icon_size) // 2, (height - icon_size) // 2))
    return canvas


save_png(assets / "Wide310x150Logo.scale-200.png", place_on_canvas(620, 300, 220))
save_png(assets / "SplashScreen.scale-200.png", place_on_canvas(1240, 600, 320))

if raw.exists():
    raw.unlink()
    print("removed raw")
