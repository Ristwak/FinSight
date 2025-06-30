using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnimatedLineGraph : MonoBehaviour
{
    [Header("Reference")]
    public SoundManager soundManager;
    public GameObject homeUI;

    [Header("Graph Settings")]
    public RectTransform graphContainer;
    public Sprite dotSprite;
    public Font labelFont;
    public LevelLoader levelLoader;

    [Header("Axes Settings")]
    public int totalLevels = 16; // X-Axis
    public float yMin = -50f;
    public float yMax = 100f;

    [Header("Appearance")]
    public Color dotColor = Color.red;
    public Color lineColor = Color.green;
    public Vector2 dotSize = new Vector2(12, 12);
    public float gridLineWidth = 1f;
    public Color gridLineColor = new Color(0.8f, 0.8f, 0.8f, 0.8f);

    [Header("Tooltip")]
    public GameObject tooltipObject;
    public Text tooltipText;
    public Vector2 tooltipOffset = new Vector2(40, -30);

    [Header("Animation")]
    public float animationSpeed = 2.0f;

    public GraphInfoPanel infoPanel; // drag this in via Inspector

    private List<float> points = new List<float>();
    private List<GameObject> dotObjects = new List<GameObject>();
    private List<GameObject> lineObjects = new List<GameObject>();
    private List<GameObject> gridLines = new List<GameObject>();

    void Start()
    {
        tooltipObject.SetActive(false);
        DrawGrid();
    }

    void Update()
    {
        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            graphContainer, Input.mousePosition, null, out localMousePos);

        float threshold = 20f;
        bool hovering = false;

        foreach (var dot in dotObjects)
        {
            RectTransform dotRT = dot.GetComponent<RectTransform>();
            Vector2 dotPos = dotRT.anchoredPosition;
            float dist = Vector2.Distance(localMousePos, dotPos);
            if (dist <= threshold)
            {
                float yValue = Mathf.Lerp(yMin, yMax, dotPos.y / graphContainer.rect.height);
                int levelIndex = dotObjects.IndexOf(dot);

                Vector2 anchoredPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    tooltipObject.transform.parent as RectTransform,
                    Input.mousePosition,
                    null,
                    out anchoredPos);

                tooltipObject.SetActive(true);
                tooltipObject.GetComponent<RectTransform>().anchoredPosition = anchoredPos + tooltipOffset;
                // tooltipText.text = $"X: {levelIndex}\nY: {Mathf.RoundToInt(yValue)}";
                tooltipText.text = $"{{X,Y}}: {{{levelIndex},{Mathf.RoundToInt(yValue)}}}";
                hovering = true;
                break;
            }
        }

        if (!hovering)
            tooltipObject.SetActive(false);
    }

    public void ResetGraph()
    {
        points.Clear();
        foreach (var obj in dotObjects) Destroy(obj);
        foreach (var obj in lineObjects) Destroy(obj);
        foreach (var line in gridLines) Destroy(line);
        dotObjects.Clear();
        lineObjects.Clear();
        gridLines.Clear();
        DrawGrid();
    }

    public void AddPoint(float profit)
    {
        points.Add(profit);
        RedrawGraph();
    }

    public void AddProfitPoint(float profit)
    {
        if (points.Count == 0)
            points.Add(0); // Always start at zero

        points.Add(profit);
        RedrawGraph();
    }

    void RedrawGraph()
    {
        StopAllCoroutines();
        foreach (var dot in dotObjects) Destroy(dot);
        foreach (var line in lineObjects) Destroy(line);
        dotObjects.Clear();
        lineObjects.Clear();

        StartCoroutine(AnimateGraph());

        // 👇 Call info panel update here (if set)
        if (infoPanel != null && points.Count > 0)
        {
            int currentBalance = Mathf.RoundToInt(points[^1]); // last point
            int maxBalance = Mathf.RoundToInt(Mathf.Max(points.ToArray()));
            int bankBalance = levelLoader.GetBankTotal();

            infoPanel.UpdateInfo(currentBalance, bankBalance, maxBalance);
        }
    }

    IEnumerator AnimateGraph()
    {
        float width = graphContainer.rect.width;
        float height = graphContainer.rect.height;
        float xSpacing = width / Mathf.Max(1, totalLevels - 1);

        for (int i = 0; i < points.Count; i++)
        {
            float xPos = i * xSpacing;
            float yRange = yMax - yMin;
            float yPos = ((points[i] - yMin) / yRange) * height;

            GameObject dot = CreateDot(new Vector2(xPos, yPos));
            dotObjects.Add(dot);

            if (i > 0)
            {
                Vector2 prev = dotObjects[i - 1].GetComponent<RectTransform>().anchoredPosition;
                Vector2 curr = dot.GetComponent<RectTransform>().anchoredPosition;
                yield return StartCoroutine(AnimateLine(prev, curr));
            }
            else
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    GameObject CreateDot(Vector2 pos)
    {
        GameObject dot = new GameObject("Dot", typeof(Image));
        dot.transform.SetParent(graphContainer, false);
        Image img = dot.GetComponent<Image>();
        img.sprite = dotSprite;
        img.color = dotColor;
        RectTransform rt = dot.GetComponent<RectTransform>();
        rt.sizeDelta = dotSize;
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.anchoredPosition = pos;
        return dot;
    }

    IEnumerator AnimateLine(Vector2 from, Vector2 to)
    {
        GameObject line = new GameObject("Line", typeof(Image));
        line.transform.SetParent(graphContainer, false);
        Image img = line.GetComponent<Image>();
        img.color = lineColor;
        RectTransform rt = line.GetComponent<RectTransform>();

        float dist = Vector2.Distance(from, to);
        Vector2 dir = (to - from).normalized;

        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.sizeDelta = new Vector2(0, 4);
        rt.anchoredPosition = from;
        rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        lineObjects.Add(line);

        if (homeUI != null && homeUI.activeInHierarchy)
        {
            soundManager.PlayGraphDrawSound();
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * animationSpeed;
            float curLength = Mathf.Lerp(0, dist, t);
            rt.sizeDelta = new Vector2(curLength, 4);
            rt.anchoredPosition = from + dir * (curLength * 0.5f);
            yield return null;
        }
    }

    void DrawGrid()
    {
        float width = graphContainer.rect.width;
        float height = graphContainer.rect.height;
        float xSpacing = width / Mathf.Max(1, totalLevels - 1);
        float yRange = yMax - yMin;
        int yDivs = 5;

        for (int i = 0; i < totalLevels; i++)
        {
            float x = i * xSpacing;
            gridLines.Add(CreateGridLine(new Vector2(x, 0), new Vector2(x, height)));
        }

        for (int i = 0; i <= yDivs; i++)
        {
            float yVal = yMin + (yRange / yDivs) * i;
            float y = ((yVal - yMin) / yRange) * height;
            gridLines.Add(CreateGridLine(new Vector2(0, y), new Vector2(width, y)));
        }
    }

    GameObject CreateGridLine(Vector2 start, Vector2 end)
    {
        GameObject obj = new GameObject("GridLine", typeof(Image));
        obj.transform.SetParent(graphContainer, false);
        Image img = obj.GetComponent<Image>();
        img.color = gridLineColor;

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        Vector2 dir = (end - start).normalized;
        float dist = Vector2.Distance(start, end);
        rt.sizeDelta = new Vector2(dist, gridLineWidth);
        rt.anchoredPosition = start + dir * (dist * 0.5f);
        rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        return obj;
    }
}
