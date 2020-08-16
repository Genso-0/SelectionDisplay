using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectionDisplayData
{
    public SelectionDisplayData(GameObject selectionDisplay, Canvas parentCanvas)
    {
        rectTransform = selectionDisplay.GetComponent<RectTransform>();
        textObj = selectionDisplay.GetComponentInChildren<TMP_Text>();
        rectTransform.gameObject.transform.SetParent(parentCanvas.transform);
    }
    public SelectionDisplayData(GameObject selectionDisplay, MeshRenderer selectedObject, Canvas parentCanvas)
    {
        rectTransform = selectionDisplay.GetComponent<RectTransform>();
        textObj = selectionDisplay.GetComponentInChildren<TMP_Text>();
        rectTransform.gameObject.transform.SetParent(parentCanvas.transform);
        this.meshRendererOnObject = selectedObject;
    }
    public TMP_Text textObj;
    public RectTransform rectTransform;
    public MeshRenderer meshRendererOnObject;
}

public class SelectionIndicatorManager : MonoBehaviour
{
    public enum TypeOfTracking
    {
        None,
        TrackWithMouseCursorOver,
        TrackAll
    }
    Canvas canvasInScene;

    public TypeOfTracking typeOfTracking;
    TypeOfTracking previousTypeOfTracking;
    public bool findGameObjectsToTrack;
    public GameObject selectionIndicatorPrefab;
    SelectionDisplayData singleMouseOverIndicator;
    List<SelectionDisplayData> selectionDisplayDatas;
    Camera cam;
    bool initialised = false;
    void Start()
    {
        cam = Camera.main;
        if(cam == null)
        {
            Debug.LogError("No camera found. Disabling manager");
            return;
        }
        canvasInScene = FindObjectOfType<Canvas>();
        if(canvasInScene == null)
        {
            Debug.LogError("No Canvas found. Disabling manager");
            return;
        }
        singleMouseOverIndicator = new SelectionDisplayData(Instantiate(selectionIndicatorPrefab), canvasInScene);
        singleMouseOverIndicator.rectTransform.gameObject.SetActive(false);
        selectionDisplayDatas = new List<SelectionDisplayData>();
        initialised = true;
    }

    void Update()
    {
        if(initialised)
        {
            if (findGameObjectsToTrack)
                FindAllObjectsAndAssignDisplayRects();
            if (previousTypeOfTracking != typeOfTracking)
                TypeOfTrackingChanged();
            switch (typeOfTracking)
            {
                case TypeOfTracking.None:
                    break;
                case TypeOfTracking.TrackWithMouseCursorOver:
                    TrackMouse();
                    break;
                case TypeOfTracking.TrackAll:
                    TrackAll();
                    break;
            }
        }
    }
    private void FindAllObjectsAndAssignDisplayRects()
    {
        selectionDisplayDatas.Clear();
        var objects = FindObjectsOfType<GameObject>();
        for (int i = 0; i < objects.Length; i++)
        {
            var meshRenderer = objects[i].GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                selectionDisplayDatas.Add(new SelectionDisplayData(Instantiate(selectionIndicatorPrefab), meshRenderer, canvasInScene));
                if (typeOfTracking != TypeOfTracking.TrackAll)
                    selectionDisplayDatas[i].rectTransform.gameObject.SetActive(false);
            }
        }
        findGameObjectsToTrack = false;
    }
    private void TypeOfTrackingChanged()
    {
        if (typeOfTracking == TypeOfTracking.TrackAll)
            TurnOnTrackAllDisplays();
        else if (typeOfTracking == TypeOfTracking.TrackWithMouseCursorOver)
            TurnOffTrackAllDisplays();
        else if (typeOfTracking == TypeOfTracking.None)
            TurnOffTrackAllDisplays();
        previousTypeOfTracking = typeOfTracking;
    }
    private void TurnOnTrackAllDisplays()
    {
        for (int i = 0; i < selectionDisplayDatas.Count; i++)
        {
            selectionDisplayDatas[i].rectTransform.gameObject.SetActive(true);
        }
    }
    private void TurnOffTrackAllDisplays()
    {
        for (int i = 0; i < selectionDisplayDatas.Count; i++)
        {
            selectionDisplayDatas[i].rectTransform.gameObject.SetActive(false);
        }
    }
    private void TrackAll()
    {
        for (int i = 0; i < selectionDisplayDatas.Count; i++)
        {
            if (IsObjectInViewOfCamera(selectionDisplayDatas[i].meshRendererOnObject.gameObject))
            {
                selectionDisplayDatas[i].rectTransform.gameObject.SetActive(true);
                SetDisplaySizeAndPosition(selectionDisplayDatas[i].meshRendererOnObject, selectionDisplayDatas[i].rectTransform);
                SetText(selectionDisplayDatas[i]);
            }
            else
            {
                selectionDisplayDatas[i].rectTransform.gameObject.SetActive(false);
            }
        }
    }
    private bool IsObjectInViewOfCamera(GameObject gameObj)
    {
        var toTarget = gameObj.transform.position - cam.transform.position;
        if (Vector3.Dot((toTarget).normalized, cam.transform.forward) > Math.Cos(cam.fieldOfView * Mathf.Deg2Rad))
        {
            return true;
        }
        else
            return false;
    }
    private void TrackMouse()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
        if (Physics.Raycast(ray, out hitInfo))
        {
            var gameObj = hitInfo.transform.gameObject;
            var renderer = gameObj.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = gameObj.GetComponentInChildren<MeshRenderer>();
            }
            if(renderer != null)
            {
                singleMouseOverIndicator.rectTransform.gameObject.SetActive(true);
                SetDisplaySizeAndPosition(renderer, singleMouseOverIndicator.rectTransform);
                singleMouseOverIndicator.meshRendererOnObject = renderer;
                SetText(singleMouseOverIndicator);
            }
        }
        else
            singleMouseOverIndicator.rectTransform.gameObject.SetActive(false);
    }
    void SetText(SelectionDisplayData selectionDisplayData)
    {
        selectionDisplayData.textObj.text = $"{selectionDisplayData.rectTransform.gameObject.name}\n" +
            $"Position {selectionDisplayData.meshRendererOnObject.gameObject.transform.position}\n" +
            $"Rotation {selectionDisplayData.meshRendererOnObject.gameObject.transform.rotation}\n" +
            $"Distance from camera {Vector3.Distance(selectionDisplayData.meshRendererOnObject.gameObject.transform.position, cam.transform.position)}";
    }
    void SetDisplaySizeAndPosition(Renderer renderer, RectTransform rt)
    {
        Matrix4x4 viewPortMatrix = cam.projectionMatrix * cam.worldToCameraMatrix;
        int scaled_pixelWidth = cam.scaledPixelWidth;
        int scaled_pixelHeight = cam.scaledPixelHeight;
        Bounds rendererBounds = renderer.bounds;

        //Create the 8 positions that make up our render bounds.
        Vector3[] boundPoints = new Vector3[8];
        boundPoints[0] = new Vector3(rendererBounds.min.x, rendererBounds.min.y, rendererBounds.min.z);
        boundPoints[1] = new Vector3(rendererBounds.max.x, rendererBounds.max.y, rendererBounds.max.z);
        boundPoints[2] = new Vector3(boundPoints[0].x, boundPoints[0].y, boundPoints[1].z);
        boundPoints[3] = new Vector3(boundPoints[0].x, boundPoints[1].y, boundPoints[0].z);
        boundPoints[4] = new Vector3(boundPoints[1].x, boundPoints[0].y, boundPoints[0].z);
        boundPoints[5] = new Vector3(boundPoints[0].x, boundPoints[1].y, boundPoints[1].z);
        boundPoints[6] = new Vector3(boundPoints[1].x, boundPoints[0].y, boundPoints[1].z);
        boundPoints[7] = new Vector3(boundPoints[1].x, boundPoints[1].y, boundPoints[0].z);

        //Convert world space points to viewport space
        for (int i = 0; i < boundPoints.Length; i++)
        {
            Vector4 boundPoint = viewPortMatrix * new Vector4(boundPoints[i].x, boundPoints[i].y, boundPoints[i].z, 1f);//converted to view port space
            boundPoint.x = (boundPoint.x / boundPoint.w + 1f) * .5f * scaled_pixelWidth; //converted  x to raster space
            boundPoint.y = (boundPoint.y / boundPoint.w + 1f) * .5f * scaled_pixelHeight;//converted y to raster space
            boundPoints[i] = new Vector3(boundPoint.x, boundPoint.y, boundPoint.z);//raster point 
        }
        //find the min and max of the raster space bounding box
        Vector3 min = boundPoints[0];
        Vector3 max = boundPoints[0];
        for (int i = 1; i < boundPoints.Length; i++)
        {
            Vector3 boundsPoint = boundPoints[i];
            for (int n = 0; n < 3; n++)
            {
                if (boundsPoint[n] > max[n])
                    max[n] = boundsPoint[n];
                if (boundsPoint[n] < min[n])
                    min[n] = boundsPoint[n];
            }
        }
        //set new rectangle dimensions in raster space!
        rt.position = new Vector2(min.x, min.y);
        rt.sizeDelta = new Vector2(max.x - min.x, max.y - min.y);
    }

    Texture2D imageUsedForTargetReticles; //make sure to set this image(i.e make it public and set from the editor) when spawning selection indicator from code.
    RectTransform CreateRectObjectDisplayFromCode()
    {
        //Add required components
        var targetReticleContainerParent = new GameObject("Target Reticle");
        var rectTransformParent = targetReticleContainerParent.AddComponent<RectTransform>();
        targetReticleContainerParent.transform.SetParent(canvasInScene.transform);
        rectTransformParent.anchorMin = Vector2.zero;
        rectTransformParent.anchorMax = Vector2.zero;
        rectTransformParent.pivot = Vector2.zero;
        rectTransformParent.offsetMax = Vector2.zero;
        rectTransformParent.offsetMin = Vector2.zero;

        //Set image components.
        var targetReticle = new GameObject("Image");
        targetReticle.transform.SetParent(targetReticleContainerParent.transform);
        var rectTransform = targetReticle.AddComponent<RectTransform>();
        targetReticle.AddComponent<CanvasRenderer>();
        var imageComponent = targetReticle.AddComponent<Image>();
        imageComponent.sprite = Sprite.Create(imageUsedForTargetReticles, new Rect(0, 0, imageUsedForTargetReticles.width, imageUsedForTargetReticles.height), new Vector2(0.5f, 0.5f),
            200, 0, SpriteMeshType.Tight, new Vector4(10, 10, 10, 10));
        imageComponent.type = Image.Type.Sliced;
        imageComponent.fillCenter = false;
        imageComponent.pixelsPerUnitMultiplier = 1;

        //Set image rect anchors and offset
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.offsetMin = Vector2.zero;

        return rectTransformParent;
    }
}
