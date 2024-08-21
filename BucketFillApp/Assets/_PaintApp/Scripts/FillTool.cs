using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FillTool : MonoBehaviour
{
	[SerializeField] private Image baseImage;
	[SerializeField] private Image _paintingImage;
	[SerializeField] private Color fillColor;
	[SerializeField] private float paintingTolerance = 0.1f;
	[SerializeField] private Color ignoringColor;
	[SerializeField] private float ignoringTolerance = 0.1f;

	private Texture2D baseTexture;
	private Texture2D paintingTexture;

	public void ColorButtonClicked(Image imgColor) {
		fillColor = imgColor.color;
	}
	private Vector2Int GetMousePixelPosition() {
		RectTransform rectTransform = baseImage.GetComponent<RectTransform>();

		// Convert screen point to local point in the RectTransform
		Vector2 localPoint;
		if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, null, out localPoint)) {
			// Calculate the actual displayed area of the image
			float aspectRatio = (float)baseTexture.width / baseTexture.height;
			float rectAspectRatio = rectTransform.rect.width / rectTransform.rect.height;

			float displayedWidth, displayedHeight;
			if (aspectRatio > rectAspectRatio) {
				displayedWidth = rectTransform.rect.width;
				displayedHeight = rectTransform.rect.width / aspectRatio;
			}
			else {
				displayedWidth = rectTransform.rect.height * aspectRatio;
				displayedHeight = rectTransform.rect.height;
			}

			float xOffset = (rectTransform.rect.width - displayedWidth) * 0.5f;
			float yOffset = (rectTransform.rect.height - displayedHeight) * 0.5f;

			Vector2 clickedPosition = new Vector2((rectTransform.rect.width / 2f) + localPoint.x, (rectTransform.rect.height / 2f) + localPoint.y);

			// Adjust local point to consider the offsets
			float x = (clickedPosition.x - xOffset) * baseTexture.width / displayedWidth;
			float y = (clickedPosition.y - yOffset) * baseTexture.height / displayedHeight;

			// Flip y-coordinate to match texture's coordinate system
			//y = texture.height - y;

			//return new Vector2Int(Mathf.Clamp(Mathf.RoundToInt(x), 0, baseTexture.width - 1), Mathf.Clamp(Mathf.RoundToInt(y), 0, baseTexture.height - 1));
			return new Vector2Int(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
		}
		return new Vector2Int(-1, -1); // Return invalid coordinate if not within the rect
	}
	private bool AreColorsEqual(Color a, Color b, float tolerance) {
		return Mathf.Abs(a.r - b.r) <= tolerance &&
			   Mathf.Abs(a.g - b.g) <= tolerance &&
			   Mathf.Abs(a.b - b.b) <= tolerance &&
			   Mathf.Abs(a.a - b.a) <= tolerance;
	}
	private void FloodFill(Texture2D baseTexture, Texture2D paintTexture, int x, int y, Color newColor) {
		Color[] paintingPixels = paintTexture.GetPixels();
		Color originalColor = paintingPixels[y * paintTexture.width + x];

		Color[] basePixels = baseTexture.GetPixels();
		Color comparingColor = basePixels[y * baseTexture.width + x];
		//if (AreColorsEqual(ignoringColor, comparingColor, ignoringTolerance)) {
		//	return;
		//}

		if (AreColorsEqual(originalColor, newColor, paintingTolerance))
			return;

		int width = paintTexture.width;
		int height = paintTexture.height;
		Stack<Vector2Int> stack = new Stack<Vector2Int>();
		stack.Push(new Vector2Int(x, y));

		while (stack.Count > 0) {
			Vector2Int point = stack.Pop();
			int px = point.x;
			int py = point.y;

			if (px < 0 || px >= width || py < 0 || py >= height)
				continue;

			if (!AreColorsEqual(paintingPixels[py * width + px], originalColor, paintingTolerance)
				|| AreColorsEqual(basePixels[py * width + px], ignoringColor, ignoringTolerance))
				continue;

			paintingPixels[py * width + px] = newColor;

			stack.Push(new Vector2Int(px - 1, py));
			stack.Push(new Vector2Int(px + 1, py));
			stack.Push(new Vector2Int(px, py - 1));
			stack.Push(new Vector2Int(px, py + 1));
		}

		paintTexture.SetPixels(paintingPixels);
	}

	private void Start() {
		if (baseImage != null && baseImage.sprite != null) {
			Texture2D originalBaseTexture = baseImage.sprite.texture;
			baseTexture = new Texture2D(originalBaseTexture.width, originalBaseTexture.height, TextureFormat.RGBA32, false);
			baseTexture.SetPixels(originalBaseTexture.GetPixels());
			baseTexture.Apply();
			baseImage.sprite = Sprite.Create(baseTexture, new Rect(0, 0, baseTexture.width, baseTexture.height), new Vector2(0.5f, 0.5f));

			Texture2D originalPaintingTexture = _paintingImage.sprite.texture;
			paintingTexture = new Texture2D(originalPaintingTexture.width, originalPaintingTexture.height, TextureFormat.RGBA32, false);
			paintingTexture.SetPixels(originalPaintingTexture.GetPixels());
			paintingTexture.Apply();
			_paintingImage.sprite = Sprite.Create(paintingTexture, new Rect(0, 0, paintingTexture.width, paintingTexture.height), new Vector2(0.5f, 0.5f));
		}
	}

	private void Update() {
		if (Input.GetMouseButtonDown(0)) // Detect left mouse button click
		{
			Vector2Int pixelUV = GetMousePixelPosition();
			if (pixelUV.x >= 0 && pixelUV.x < baseTexture.width && pixelUV.y >= 0 && pixelUV.y < baseTexture.height) {
				FloodFill(baseTexture, paintingTexture, pixelUV.x, pixelUV.y, fillColor);
				paintingTexture.Apply();
			}
		}
	}
}
