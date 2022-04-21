using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.UI;

public class PooledThumbnailManager : MonoBehaviour
{
	[Header("Pool Scroll Rect")]
	public RectTransform container;
	public ScrollRect scrollRect;
	public GridLayoutGroup gridLayoutGroup;
	public float poolThreshold;
	
	[Header("Thumbnail")]
	public GameObject prefab;
	public int thumbnailCount = 1000;

	private readonly List<ThumbnailVO> _thumbnailVOList = new List<ThumbnailVO>();
	private readonly List<RectTransform> _activeThumbnails = new List<RectTransform>();
	private int _lastCulledAbove;
	private int _numThumbnailsVisibleInScrollArea;
	private int _requiredThumbnailsInList;

	private Bounds _scrollRectBounds;
	private Vector3[] _corners = new Vector3[4];

	private float _lastDeltaY;

	private float _cellTotalHeight => gridLayoutGroup.cellSize.y + gridLayoutGroup.spacing.y;

	private enum PoolMethod
	{
		TopToBottom,
		BottomToTop
	}

	private void Start()
	{
		CreateThumbnailVOList();
		AdjustContentSize();
		InitNumThumbnailsVisibleInScrollArea();
		SetScrollRectBounds();
		InitializeThumbnails(_requiredThumbnailsInList);
		ScrollMoved(Vector2.zero);
	}

	private void ScrollMoved(Vector2 delta)
	{
		int numThumbnailsCulledAbove = _lastCulledAbove;

		if (_lastDeltaY - delta.y > 0 && _activeThumbnails[0].MinY() > _scrollRectBounds.max.y)
			numThumbnailsCulledAbove += gridLayoutGroup.constraintCount;
		else if (_lastDeltaY - delta.y < 0 && _activeThumbnails[_activeThumbnails.Count - 1].MaxY() < _scrollRectBounds.min.y)
			numThumbnailsCulledAbove -= gridLayoutGroup.constraintCount;

		numThumbnailsCulledAbove = Mathf.Clamp(numThumbnailsCulledAbove, 0, thumbnailCount - (_numThumbnailsVisibleInScrollArea + gridLayoutGroup.constraintCount));
		
		bool refreshRequired = numThumbnailsCulledAbove - _lastCulledAbove != 0;
		if (refreshRequired)
		{
			PoolMethod poolMethod = numThumbnailsCulledAbove > _lastCulledAbove ? PoolMethod.TopToBottom : PoolMethod.BottomToTop;
			PoolThumbnail(poolMethod, numThumbnailsCulledAbove);

			_lastCulledAbove = numThumbnailsCulledAbove;
		}

		_lastDeltaY = delta.y;
	}

	private void InitializeThumbnails(int requiredElementsInList)
	{
		for (int i = 0; i < requiredElementsInList; i++)
		{
			GameObject gameObj = Instantiate(prefab, container, false);
			gameObj.GetComponent<Thumbnail>().thumbnailVO = _thumbnailVOList[i];
			_activeThumbnails.Add((RectTransform)gameObj.transform);
		}
	}

	private void PoolThumbnail(PoolMethod repurposeMethod, int numElementsCulledAbove)
	{
		if (repurposeMethod == PoolMethod.TopToBottom)
		{
			for (int i = 0; i < gridLayoutGroup.constraintCount; i++)
			{
				int index = numElementsCulledAbove + _numThumbnailsVisibleInScrollArea + i;
				if (index >= _thumbnailVOList.Count)
					break;

				RectTransform top = _activeThumbnails[0];
				_activeThumbnails.RemoveAt(0);
				top.anchoredPosition = new Vector2(top.anchoredPosition.x, _activeThumbnails[_activeThumbnails.Count - 1 - i].anchoredPosition.y - _cellTotalHeight);
				_activeThumbnails.Add(top);
				top.GetComponent<Thumbnail>().thumbnailVO = _thumbnailVOList[index];
			}
		}
		else
		{
			for (int i = 0; i < gridLayoutGroup.constraintCount; i++)
			{
				int index = _lastCulledAbove - i - 1;
				if (index < 0)
					break;

				RectTransform bottom = _activeThumbnails[_activeThumbnails.Count - 1];
				_activeThumbnails.RemoveAt(_activeThumbnails.Count - 1);
				bottom.anchoredPosition = new Vector2(bottom.anchoredPosition.x, _activeThumbnails[gridLayoutGroup.constraintCount - 1].anchoredPosition.y + _cellTotalHeight);
				_activeThumbnails.Insert(0, bottom);
				bottom.GetComponent<Thumbnail>().thumbnailVO = _thumbnailVOList[index];
			}
		}
	}

	private void SetScrollRectBounds()
	{
		_corners = ((RectTransform)scrollRect.transform).GetCorners();
		float threshHold = poolThreshold * (_corners[2].y - _corners[0].y);
		_scrollRectBounds.min = new Vector3(_corners[0].x, _corners[0].y - threshHold);
		_scrollRectBounds.max = new Vector3(_corners[2].x, _corners[2].y + threshHold);
	}

	private void AdjustContentSize()
	{
		Vector2 currentSize = container.sizeDelta;
		int numRow = Mathf.CeilToInt(thumbnailCount / (float)gridLayoutGroup.constraintCount);

		float size = numRow * _cellTotalHeight - gridLayoutGroup.spacing.y;
		currentSize.y = size;

		container.sizeDelta = currentSize;
	}

	private void InitNumThumbnailsVisibleInScrollArea()
	{
		//How many elements can fit into the content area
		float scrollAreaSize = scrollRect.viewport.rect.height;
		_numThumbnailsVisibleInScrollArea = Mathf.CeilToInt(scrollAreaSize / _cellTotalHeight) * gridLayoutGroup.constraintCount;
		_requiredThumbnailsInList = Mathf.Min((_numThumbnailsVisibleInScrollArea + gridLayoutGroup.constraintCount), thumbnailCount);
	}

	private void CreateThumbnailVOList()
	{
		for (int i = 0; i < thumbnailCount; i++)
		{
			var thumbnailVO = new ThumbnailVO();
			thumbnailVO.id = i.ToString();
			_thumbnailVOList.Add(thumbnailVO);
		}
	}

	private void OnEnable()
	{
		if (scrollRect == null)
			scrollRect = GetComponent<ScrollRect>();

		scrollRect.onValueChanged.AddListener(ScrollMoved);
	}

	private void OnDisable()
	{
		scrollRect.onValueChanged.RemoveListener(ScrollMoved);
	}
}
