from pathlib import Path
from PIL import Image

assets = Path(__file__).resolve().parents[1] / "src" / "Panosse.WinUI" / "Assets"
src = Image.open(assets / "panosse.png").convert("RGBA")


def fit_square(size: int) -> Image.Image:
    return src.resize((size, size), Image.Resampling.LANCZOS)


def save_png(path: Path, img: Image.Image) -> None:
    img.save(path, format="PNG")
    print(f"wrote {path.name} {img.size}")


# Pillow generates all listed sizes from the source image.
ico_sizes = [(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
fit_square(256).save(assets / "panosse.ico", format="ICO", sizes=ico_sizes)
print(f"wrote panosse.ico sizes={[s[0] for s in ico_sizes]}")

# Verify
with Image.open(assets / "panosse.ico") as ico:
    sizes = sorted(ico.info.get("sizes", []), reverse=True)
    print(f"verified ico sizes={sizes} bytes={(assets / 'panosse.ico').stat().st_size}")

save_png(assets / "StoreLogo.png", fit_square(50))
save_png(assets / "Square44x44Logo.scale-200.png", fit_square(88))
save_png(assets / "Square44x44Logo.targetsize-24_altform-unplated.png", fit_square(24))
save_png(assets / "Square150x150Logo.scale-200.png", fit_square(300))
save_png(assets / "LockScreenLogo.scale-200.png", fit_square(96))


def place_on_canvas(width: int, height: int, icon_size: int) -> Image.Image:
    canvas = Image.new("RGBA", (width, height), (33, 150, 243, 255))
    icon = fit_square(icon_size)
    x = (width - icon_size) // 2
    y = (height - icon_size) // 2
    canvas.alpha_composite(icon, (x, y))
    return canvas


save_png(assets / "Wide310x150Logo.scale-200.png", place_on_canvas(620, 300, 220))
save_png(assets / "SplashScreen.scale-200.png", place_on_canvas(1240, 600, 320))
