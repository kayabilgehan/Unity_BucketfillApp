using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FillTool : MonoBehaviour
{
	[SerializeField] private Image paintingImage;
	[SerializeField] private Color fillColor;
	[SerializeField] private float tolerance = 0.1f;

	private Texture2D texture;


	private Vector2Int GetMousePixelPosition() {
		RectTransform rectTransform = paintingImage.GetComponent<RectTransform>();

		// Convert screen point to local point in the RectTransform
		Vector2 localPoint;
		if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, null, out localPoint)) {
			// Calculate the actual displayed area of the image
			float aspectRatio = (float)texture.width / texture.height;
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
			float x = (clickedPosition.x - xOffset) * texture.width / displayedWidth;
			float y = (clickedPosition.y - yOffset) * texture.height / displayedHeight;

			// Flip y-coordinate to match texture's coordinate system
			//y = texture.height - y;

			return new Vector2Int(Mathf.Clamp(Mathf.RoundToInt(x), 0, texture.width - 1), Mathf.Clamp(Mathf.RoundToInt(y), 0, texture.height - 1));
		}
		return new Vector2Int(-1, -1); // Return invalid coordinate if not within the rect
	}
	private bool AreColorsEqual(Color a, Color b) {
		return Mathf.Abs(a.r - b.r) <= tolerance &&
			   Mathf.Abs(a.g - b.g) <= tolerance &&
			   Mathf.Abs(a.b - b.b) <= tolerance &&
			   Mathf.Abs(a.a - b.a) <= tolerance;
	}
	private void FloodFill(Texture2D texture, int x, int y, Color newColor) {
		Color[] pixels = texture.GetPixels();
		Color originalColor = pixels[y * texture.width + x];

		if (AreColorsEqual(originalColor, newColor))
			return;

		int width = texture.width;
		int height = texture.height;
		Stack<Vector2Int> stack = new Stack<Vector2Int>();
		stack.Push(new Vector2Int(x, y));

		while (stack.Count > 0) {
			Vector2Int point = stack.Pop();
			int px = point.x;
			int py = point.y;

			if (px < 0 || px >= width || py < 0 || py >= height)
				continue;

			if (!AreColorsEqual(pixels[py * width + px], originalColor))
				continue;

			pixels[py * width + px] = newColor;

			stack.Push(new Vector2Int(px - 1, py));
			stack.Push(new Vector2Int(px + 1, py));
			stack.Push(new Vector2Int(px, py - 1));
			stack.Push(new Vector2Int(px, py + 1));
		}

		texture.SetPixels(pixels);
	}

	private void Start() {
		if (paintingImage != null && paintingImage.sprite != null) {
			Texture2D originalTexture = paintingImage.sprite.texture;
			texture = new Texture2D(originalTexture.width, originalTexture.height, TextureFormat.RGBA32, false);
			texture.SetPixels(originalTexture.GetPixels());
			texture.Apply();
			paintingImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
		}
	}

	private void Update() {
		if (Input.GetMouseButtonDown(0)) // Detect left mouse button click
		{
			Vector2Int pixelUV = GetMousePixelPosition();
			if (pixelUV.x >= 0 && pixelUV.x < texture.width && pixelUV.y >= 0 && pixelUV.y < texture.height) {
				FloodFill(texture, pixelUV.x, pixelUV.y, fillColor);
				texture.Apply();
			}
		}
	}
}
