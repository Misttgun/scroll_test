using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ThumbnailManager:MonoBehaviour {

	public Transform container;
	public GameObject prefab;
	public int count = 1000;

	private List<ThumbnailVO> _thumbnailVOList = new List<ThumbnailVO>();
	
	void Start () {
		createThumbnailVOList();
		createThumbnailPrefabs();
    }
	
	private void createThumbnailVOList() {
		ThumbnailVO thumbnailVO;
		for (int i=0; i<count; i++) {
			thumbnailVO = new ThumbnailVO();
			thumbnailVO.id = i.ToString();
            _thumbnailVOList.Add(thumbnailVO);
        }
	}

	private void createThumbnailPrefabs() {
		GameObject gameObj;
		for (int i = 0; i < _thumbnailVOList.Count; i++) {
			gameObj = (GameObject)Instantiate(prefab);
			gameObj.transform.SetParent(container, false);
			gameObj.GetComponent<Thumbnail>().thumbnailVO = _thumbnailVOList[i];
        }
	}
}
