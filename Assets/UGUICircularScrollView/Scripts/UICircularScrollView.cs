//*****************************-》 基类 循环列表 《-****************************
//author kim
//初始化:
//      Init(callBackFunc)
//刷新整个列表（首次调用和数量变化时调用）:
//      ShowList(int = 数量)
//刷新单个项:
//      UpdateCell(int = 索引)
//刷新列表数据(无数量变化时调用):
//      UpdateList()
//回调:
//Func(GameObject = Cell, int = Index)  //刷新列表

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems;
using System;

namespace CircularScrollView
{

    public class UIUtils
    {
        public static void SetActive(GameObject obj, bool isActive)
        {
            if (obj != null)
            {
                obj.SetActive(isActive);
            }

        }
    }

    public enum e_Direction
    {
        Horizontal,
        Vertical
    }

    /// <summary>
    /// 如有空指针错误，请查验是否init();
    /// （ps:CellItem、Content 的Anchors 需为Top-Left Pivot为（0，1））
    /// </summary>
    public class UICircularScrollView : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        //protected void OnEnable()
        //{

        //}

        public GameObject m_PointingFirstArrow;
        public GameObject m_PointingEndArrow;

        /// <summary>
        /// Horizontal横向滑动
        /// </summary>
        public e_Direction m_Direction = e_Direction.Horizontal;
        public bool m_IsShowArrow = true;

        public int m_Row = 1;

        public float m_Spacing = 0f; //间距
        public GameObject m_CellGameObject; //指定的cell
        /// <summary>
        /// 刷新item
        /// </summary>
        protected Action<GameObject, int> m_FuncCallBackFunc;
        protected Action<GameObject, int> m_FuncOnClickCallBack;
        protected Action<int, bool, GameObject> m_FuncOnButtonClickCallBack;
        protected Action<List<CellInfo>> m_DoShowAnimator;
        public Action mOnScollCall;


        protected RectTransform rectTrans;

        protected float m_PlaneWidth;
        protected float m_PlaneHeight;

        protected float m_ContentWidth;
        protected float m_ContentHeight;

        protected float m_CellObjectWidth;
        protected float m_CellObjectHeight;

        protected GameObject m_Content;
        protected RectTransform m_ContentRectTrans;

        private bool m_isInited = false;

        //记录 物体的坐标 和 物体 
        public class CellInfo
        {
            public int index;
            public Vector3 pos;
            public GameObject obj;
        };

        protected CellInfo[] m_CellInfos;
        protected CellInfo[] m_RowBgInfos;

        protected bool m_IsInited = false;

        protected ScrollRect m_ScrollRect;

        protected int m_MaxCount = -1; //列表数量

        protected int m_MinIndex = -1; //可观察到的最小item 的索引 
        protected int m_MaxIndex = -1;//可观察到的最大item 的索引 

        public float m_viewIndex = 0;//可观看到的item数量
        protected bool m_IsClearList = false; //是否清空列表

        public Vector2 topPadding = Vector2.zero;
        int m_MaxLine = 1;

        public void SetShowAnimator(Action<List<CellInfo>> action)
        {
            m_DoShowAnimator = action;
        }

        public virtual void Init(Action<GameObject, int> refrshCallBack)
        {
            Init(refrshCallBack, null);
        }
        public virtual void Init(Action<GameObject, int> callBack, Action<GameObject, int> onClickCallBack, Action<int, bool, GameObject> onButtonClickCallBack)
        {
            if (onButtonClickCallBack != null)
            {
                m_FuncOnButtonClickCallBack = onButtonClickCallBack;
            }
            Init(callBack, onClickCallBack);
        }
        float defautZ = 0;
        public virtual void Init(Action<GameObject, int> callBack, Action<GameObject, int> onClickCallBack)
        {

            DisposeAll();

            m_FuncCallBackFunc = callBack;

            if (onClickCallBack != null)
            {
                m_FuncOnClickCallBack = onClickCallBack;
            }

            if (m_isInited)
                return;


            m_Content = this.GetComponent<ScrollRect>().content.gameObject;

            if (m_CellGameObject == null)
            {
                m_CellGameObject = m_Content.transform.GetChild(0).gameObject;
            }
            /* Cell 处理 */
            //m_CellGameObject.transform.SetParent(m_Content.transform.parent, false);
            SetPoolsObj(m_CellGameObject);

            RectTransform cellRectTrans = m_CellGameObject.GetComponent<RectTransform>();
            cellRectTrans.pivot = new Vector2(0f, 1f);
            CheckAnchor(cellRectTrans);
            cellRectTrans.anchoredPosition = Vector2.zero;

            //记录 Cell 信息
            m_CellObjectHeight = cellRectTrans.rect.height;
            m_CellObjectWidth = cellRectTrans.rect.width;
            defautZ = cellRectTrans.localPosition.z;
            //记录 Plane 信息
            rectTrans = GetComponent<RectTransform>();
            Rect planeRect = rectTrans.rect;
            m_PlaneHeight = planeRect.height;
            m_PlaneWidth = planeRect.width;

            //记录 Content 信息
            m_ContentRectTrans = m_Content.GetComponent<RectTransform>();
            Rect contentRect = m_ContentRectTrans.rect;
            m_ContentHeight = contentRect.height;
            m_ContentWidth = contentRect.width;

            m_ContentRectTrans.pivot = new Vector2(0f, 1f);
            //m_ContentRectTrans.sizeDelta = new Vector2 (planeRect.width, planeRect.height);
            //m_ContentRectTrans.anchoredPosition = Vector2.zero;
            CheckAnchor(m_ContentRectTrans);

            m_ScrollRect = this.GetComponent<ScrollRect>();

            m_ScrollRect.onValueChanged.RemoveAllListeners();
            //添加滑动事件
            m_ScrollRect.onValueChanged.AddListener(delegate (Vector2 value) { ScrollRectListener(value); });

            if (m_PointingFirstArrow != null || m_PointingEndArrow != null)
            {
                m_ScrollRect.onValueChanged.AddListener(delegate (Vector2 value) { OnDragListener(value); });
                OnDragListener(Vector2.zero);
            }

            //InitScrollBarGameObject(); // 废弃

            //rowBg
            if (ptrRowBg!=null)
            {
                SetPoolsRowBg(itemRowBg);
            }


            m_isInited = true;

        }

        public void scollToIndex(int index)
        {
            index = Mathf.CeilToInt(index / m_Row);
            float value = 0;
            int viewSize = 0;
            if (m_Direction == e_Direction.Vertical)
            {
                viewSize = (int)m_viewIndex;
                value = (m_MaxLine - viewSize - index) / (float)(m_MaxLine - viewSize);
                value = value < 0 ? 0 : value;
                m_ScrollRect.verticalNormalizedPosition = value;
               
            }
            else
            {
                viewSize = (int)(m_viewIndex) ;
                value = index / (float)(m_MaxLine - viewSize);
                value = value > 1 ? 1 : value;
                m_ScrollRect.horizontalNormalizedPosition = value;
            }
        }



        public int getCurFirstViewIndex()
        {
            int index = 0;
            if (m_Direction == e_Direction.Vertical)
            {
                index = Mathf.CeilToInt((m_ContentRectTrans.localPosition.y-topPadding.y) / (CellSzie));

            }
            else
            {
                index = Mathf.CeilToInt((m_ContentRectTrans.localPosition.x-topPadding.x) / (CellSzie));
            }
            index *= m_Row;
            index = index < 0 ? 0 : index;
            index = index >= m_MaxCount ? m_MaxCount-1 : index;
            return index;
        }

        //检查 Anchor 是否正确
        private void CheckAnchor(RectTransform rectTrans)
        {
            if (m_Direction == e_Direction.Vertical)
            {
                if (!((rectTrans.anchorMin == new Vector2(0, 1) && rectTrans.anchorMax == new Vector2(0, 1)) ||
                         (rectTrans.anchorMin == new Vector2(0, 1) && rectTrans.anchorMax == new Vector2(1, 1))))
                {
                    rectTrans.anchorMin = new Vector2(0, 1);
                    rectTrans.anchorMax = new Vector2(1, 1);
                }
            }
            else
            {
                if (!((rectTrans.anchorMin == new Vector2(0, 1) && rectTrans.anchorMax == new Vector2(0, 1)) ||
                         (rectTrans.anchorMin == new Vector2(0, 0) && rectTrans.anchorMax == new Vector2(0, 1))))
                {
                    rectTrans.anchorMin = new Vector2(0, 0);
                    rectTrans.anchorMax = new Vector2(0, 1);
                }
                //rectTrans.ro
            }
        }

        //实时刷新列表时用
        public virtual void UpdateList(bool force = true)
        {
            if (force)
            {
                UpdateCheck();
            }
            else
            {
                for (int i = m_MinIndex, length = m_MaxIndex; i <= length; i++)
                {
                    CellInfo cellInfo = m_CellInfos[i];
                    if (cellInfo.obj != null)
                    {
                        Func(m_FuncCallBackFunc, cellInfo);
                        //float rangePos = m_Direction == e_Direction.Vertical ? cellInfo.pos.y : cellInfo.pos.x;
                        //if (!IsOutRange(rangePos))
                        //{
                        //    Func(m_FuncCallBackFunc, cellInfo, true);
                        //}
                    }
                }
            }


        }

        //刷新某一项
        public void UpdateCell(int index)
        {
            CellInfo cellInfo = m_CellInfos[index];
            if (cellInfo.obj != null)
            {
                float rangePos = m_Direction == e_Direction.Vertical ? cellInfo.pos.y : cellInfo.pos.x;
                if (!IsOutRange(rangePos))
                {
                    Func(m_FuncCallBackFunc, cellInfo);
                }
            }
        }

        public float CellSzie{
            get
            {
                if (m_Direction == e_Direction.Vertical)
                {
                    return m_Spacing + m_CellObjectHeight;

                }
                else
                {
                    return m_Spacing + m_CellObjectWidth;
                }
            }
        }
        
        public virtual void ShowList(string numStr) { }
        
        public virtual void ShowList(int num)
        {
            if (!m_isInited)
            {
                Debug.LogError("please init first");
                return;
            }
            //m_IsClearList = true;
            m_MinIndex = -1;
            m_MaxIndex = -1;
            scollToIndex(0);
            float viewValue = 0;
            //RectTransform rectTrans = m_ContentRectTrans;
            //-> 计算 Content 尺寸
            if (m_Direction == e_Direction.Vertical)
            {
                viewValue = m_ScrollRect.viewport.rect.size.y;
                //viewValue = m_ScrollRect.viewport.sizeDelta.y;//sizeDelta在某一种情况下会为0
                m_MaxLine = Mathf.CeilToInt((float)num / m_Row);
                float contentSize = (m_Spacing + m_CellObjectHeight) * m_MaxLine+topPadding.y;
                m_ContentWidth = m_ContentRectTrans.sizeDelta.x;
                m_ContentHeight = contentSize;
                //contentSize = contentSize < rectTrans.rect.height ? rectTrans.rect.height : contentSize;
                m_ContentRectTrans.sizeDelta = new Vector2(m_ContentWidth, contentSize);
                if (num != m_MaxCount)
                {
                    m_ContentRectTrans.anchoredPosition = new Vector2(m_ContentRectTrans.anchoredPosition.x, 0) ;
                }
                m_viewIndex = viewValue / CellSzie;
            }
            else
            {
                viewValue = m_ScrollRect.viewport.rect.size.x;
                //viewValue = m_ScrollRect.viewport.sizeDelta.x;
                m_MaxLine = Mathf.CeilToInt((float)num / m_Row);
                float contentSize = (m_Spacing + m_CellObjectWidth) * m_MaxLine+topPadding.x;
                m_ContentWidth = contentSize;
                m_ContentHeight = m_ContentRectTrans.sizeDelta.y;
                //contentSize = contentSize < rectTrans.rect.width ? rectTrans.rect.width : contentSize;
                m_ContentRectTrans.sizeDelta = new Vector2(contentSize, m_ContentHeight);
                if (num != m_MaxCount)
                {
                    m_ContentRectTrans.anchoredPosition = new Vector2(0, m_ContentRectTrans.anchoredPosition.y);
                }
                m_viewIndex = viewValue / CellSzie;
            }

            //-> 计算 开始索引
            int lastEndIndex = 0;
            int starRow = 0;
            //Debug.Log(" num = " + num);
            //-> 过多的物体 扔到对象池 ( 首次调 ShowList函数时 则无效 )
            if (m_IsInited)
            {
                //Debug.Log("clear cache : num = "+ num);
                lastEndIndex = num - m_MaxCount > 0 ? m_MaxCount : num;
                lastEndIndex = m_IsClearList ? 0 : lastEndIndex;

                int count = m_IsClearList ? m_CellInfos.Length : m_MaxCount;
                for (int i = lastEndIndex; i < count; i++)
                {
                    if (m_CellInfos[i] == null)
                    {
                        continue;
                    }
                    if (m_CellInfos[i].obj != null)
                    {
                        SetPoolsObj(m_CellInfos[i].obj);
                        m_CellInfos[i].obj = null;
                    }
                }
                //rowBg
                if (ptrRowBg != null)
                {
                    starRow = Mathf.CeilToInt(lastEndIndex / m_Row);
                    int endRow = m_IsClearList ? m_RowBgInfos.Length : m_MaxLine;
                    for (int i = starRow; i < endRow; ++i)
                    {
                        if (m_RowBgInfos[i] == null)
                        {
                            continue;
                        }
                        if (m_RowBgInfos[i].obj != null)
                        {
                            SetPoolsRowBg(m_RowBgInfos[i].obj);
                            m_RowBgInfos[i].obj = null;
                        }
                    }
                }
            }

            //-> 以下四行代码 在for循环所用
            CellInfo[] tempCellInfos = m_CellInfos;
            m_CellInfos = new CellInfo[num];
            List<CellInfo> showItems = new List<CellInfo>();
            //-> 1: 计算 每个Cell坐标并存储 2: 显示范围内的 Cell
            for (int i = 0; i < num; i++)
            {
                // * -> 存储 已有的数据 ( 首次调 ShowList函数时 则无效 )
                if (m_MaxCount != -1 && i < lastEndIndex)
                {
                    CellInfo tempCellInfo = tempCellInfos[i];
                    //-> 计算是否超出范围
                    float rPos = m_Direction == e_Direction.Vertical ? tempCellInfo.pos.y : tempCellInfo.pos.x;
                    if (!IsOutRange(rPos))
                    {
                        //-> 记录显示范围中的 首位index 和 末尾index
                        m_MinIndex = m_MinIndex == -1 ? i : m_MinIndex; //首位index
                        m_MaxIndex = i; // 末尾index

                        if (tempCellInfo.obj == null)
                        {
                            tempCellInfo.obj = GetPoolsObj();
                        }
                        //tempCellInfo.obj.transform.GetComponent<RectTransform>().anchoredPosition = tempCellInfo.pos;
                        tempCellInfo.obj.transform.localPosition = tempCellInfo.pos;
                        tempCellInfo.obj.name = i.ToString();
                        tempCellInfo.obj.SetActive(true);

                        Func(m_FuncCallBackFunc, tempCellInfo);
                        showItems.Add(tempCellInfo);
                    }
                    else
                    {
                        SetPoolsObj(tempCellInfo.obj);
                        tempCellInfo.obj = null;
                    }
                    m_CellInfos[i] = tempCellInfo;
                    continue;
                }

                CellInfo cellInfo = new CellInfo();

                float pos = 0;  //坐标( isVertical ? 记录Y : 记录X )
                float rowPos = 0; //计算每排里面的cell 坐标

                // * -> 计算每个Cell坐标
                if(m_Direction == e_Direction.Vertical)
                {
                    pos = m_CellObjectHeight * Mathf.FloorToInt(i / m_Row) + m_Spacing * Mathf.FloorToInt(i / m_Row)+topPadding.y;
                    rowPos = m_CellObjectWidth * (i % m_Row) + m_Spacing * (i % m_Row)+ topPadding.x;
                    cellInfo.pos = new Vector3(rowPos, -pos, defautZ);
                }
                else
                {
                    pos = m_CellObjectWidth * Mathf.FloorToInt(i / m_Row) + m_Spacing * Mathf.FloorToInt(i / m_Row)+topPadding.x;
                    rowPos = m_CellObjectHeight * (i % m_Row) + m_Spacing * (i % m_Row)+topPadding.y;
                    cellInfo.pos = new Vector3(pos, -rowPos, defautZ);
                }
                cellInfo.index = i;
                //-> 计算是否超出范围
                float cellPos = m_Direction == e_Direction.Vertical ? cellInfo.pos.y : cellInfo.pos.x;
                if (IsOutRange(cellPos))
                {
                    cellInfo.obj = null;
                    m_CellInfos[i] = cellInfo;
                    continue;
                }

                //-> 记录显示范围中的 首位index 和 末尾index
                m_MinIndex = m_MinIndex == -1 ? i : m_MinIndex; //首位index
                m_MaxIndex = i; // 末尾index

                //-> 取或创建 Cell
                GameObject cell = GetPoolsObj();
                cell.transform.GetComponent<RectTransform>().anchoredPosition = cellInfo.pos;
                cell.gameObject.name = i.ToString();

                //-> 存数据
                cellInfo.obj = cell;
                m_CellInfos[i] = cellInfo;

                //-> 回调  函数
                Func(m_FuncCallBackFunc, cellInfo);
                showItems.Add(cellInfo);
            }
            m_MaxCount = num;
            OnDragListener(Vector2.zero);
            ShowWithAnimator(showItems);
            //if (corAnimShow!=null)
            //{
            //    StopCoroutine(corAnimShow);
            //}
            //corAnimShow = StartCoroutine (IShowWithAnimator(showItems));

            //RowBg
            if (ptrRowBg!=null)
            {
                //-> 以下四行代码 在for循环所用
                CellInfo[] tmpRowBgInfos = m_RowBgInfos;
                m_RowBgInfos = new CellInfo[m_MaxLine];
                List<CellInfo> showRowBgItems = new List<CellInfo>();
                //-> 1: 计算 每个Cell坐标并存储 2: 显示范围内的 Cell
                for (int i = 0; i < m_MaxLine; i++)
                {
                    // * -> 存储 已有的数据 ( 首次调 ShowList函数时 则无效 )
                    if (m_MaxLine != -1 && i < starRow)
                    {
                        CellInfo tempCellInfo = tmpRowBgInfos[i];
                        //-> 计算是否超出范围
                        float rPos = m_Direction == e_Direction.Vertical ? tempCellInfo.pos.y : tempCellInfo.pos.x;
                        if (!IsOutRange(rPos))
                        {
                            if (tempCellInfo.obj == null)
                            {
                                tempCellInfo.obj = GetPoolsRowBg();
                            }
                            //tempCellInfo.obj.transform.GetComponent<RectTransform>().anchoredPosition = tempCellInfo.pos;
                            tempCellInfo.obj.transform.localPosition = tempCellInfo.pos;
                            tempCellInfo.obj.name = i.ToString();
                            tempCellInfo.obj.SetActive(true);
                            showRowBgItems.Add(tempCellInfo);
                        }
                        else
                        {
                            SetPoolsRowBg(tempCellInfo.obj);
                            tempCellInfo.obj = null;
                        }
                        m_RowBgInfos[i] = tempCellInfo;
                        continue;
                    }

                    CellInfo cellInfo = new CellInfo();

                    float pos = 0;  //坐标( isVertical ? 记录Y : 记录X )

                    // * -> 计算每个Cell坐标
                    if (m_Direction == e_Direction.Vertical)
                    {
                        pos = m_CellObjectHeight * i  + m_Spacing * i +topPadding.y;
                        cellInfo.pos = new Vector3(0, -pos, defautZ);
                    }
                    else
                    {
                        pos = m_CellObjectWidth * i  + m_Spacing * i + topPadding.x;
                        cellInfo.pos = new Vector3(pos,0, defautZ);
                    }
                    cellInfo.index = i;
                    //-> 计算是否超出范围
                    float cellPos = m_Direction == e_Direction.Vertical ? cellInfo.pos.y : cellInfo.pos.x;
                    if (IsOutRange(cellPos))
                    {
                        cellInfo.obj = null;
                        m_RowBgInfos[i] = cellInfo;
                        continue;
                    }

                    //-> 取或创建 Cell
                    GameObject cell = GetPoolsRowBg();
                    cell.transform.GetComponent<RectTransform>().anchoredPosition = cellInfo.pos;
                    cell.gameObject.name = i.ToString();

                    //-> 存数据
                    cellInfo.obj = cell;
                    m_RowBgInfos[i] = cellInfo;

                    ////-> 回调  函数
                    //Func(m_FuncCallBackFunc, cellInfo);
                    showRowBgItems.Add(cellInfo);
                }
            }

            m_IsInited = true;

            
        }

        Coroutine corAnimShow;

        public IEnumerator IShowWithAnimator(List<CellInfo> items)
        {
            if (m_DoShowAnimator != null)
                m_DoShowAnimator.Invoke(items);
            yield return 1;
        }

        public void ShowWithAnimator(List<CellInfo> items)
        {
            if (m_DoShowAnimator != null)
                m_DoShowAnimator.Invoke(items);
        }

        // 更新滚动区域的大小
        void UpdateSize()
        {
            Rect rect = GetComponent<RectTransform>().rect;
            m_PlaneHeight = rect.height;
            m_PlaneWidth = rect.width;
        }
        //滑动事件
        protected virtual void ScrollRectListener(Vector2 value)
        {
            //Debug.Log("ScrollRectListener count=" + ++count);
            UpdateCheck();
            if (mOnScollCall != null)
            {
                mOnScollCall.Invoke();
            }
        }
       
        private void UpdateCheck()
        {
            if (m_CellInfos == null)
                return;
            m_MinIndex = -1;
            m_MaxIndex = -1;
            //检查超出范围
            for (int i = 0, length = m_CellInfos.Length; i < length; i++)
            {
                CellInfo cellInfo = m_CellInfos[i];
                GameObject obj = cellInfo.obj;
                Vector3 pos = cellInfo.pos;

                float rangePos = m_Direction == e_Direction.Vertical ? pos.y : pos.x;
                //判断是否超出显示范围
                if (IsOutRange(rangePos))
                {
                    //把超出范围的cell 扔进 poolsObj里
                    if (obj != null)
                    {
                        SetPoolsObj(obj);
                        m_CellInfos[i].obj = null;
                    }
                    //if (m_MaxIndex==-1&& m_MinIndex != -1&& i > m_MinIndex)
                    //{
                    //    m_MaxIndex = i;
                    //}
                }
                else
                {
                    if (obj == null)
                    {
                        //优先从 poolsObj中 取出 （poolsObj为空则返回 实例化的cell）
                        GameObject cell = GetPoolsObj();
                        cell.transform.localPosition = pos;
                        cell.gameObject.name = i.ToString();
                        m_CellInfos[i].obj = cell;

                        Func(m_FuncCallBackFunc, m_CellInfos[i]);
                    }
                    if (m_MinIndex == -1)
                    {
                        m_MinIndex = i;
                    }
                    m_MaxIndex = i;
                }
            }
            if (ptrRowBg == null)
            {
                return;
            }
            for (int i = 0, length = m_RowBgInfos.Length; i < length; i++)
            {
                CellInfo cellInfo = m_RowBgInfos[i];
                GameObject obj = cellInfo.obj;
                Vector3 pos = cellInfo.pos;

                float rangePos = m_Direction == e_Direction.Vertical ? pos.y : pos.x;
                //判断是否超出显示范围
                if (IsOutRange(rangePos))
                {
                    //把超出范围的cell 扔进 poolsObj里
                    if (obj != null)
                    {
                        SetPoolsRowBg(obj);
                        m_RowBgInfos[i].obj = null;
                    }
                }
                else
                {
                    if (obj == null)
                    {
                        //优先从 poolsObj中 取出 （poolsObj为空则返回 实例化的cell）
                        GameObject cell = GetPoolsRowBg();
                        cell.transform.localPosition = pos;
                        cell.gameObject.name = i.ToString();
                        m_RowBgInfos[i].obj = cell;
                    }
                }
            }

            //if (m_MaxIndex==-1)
            //{
            //    m_MaxIndex = m_MaxCount;
            //}
        }

        //判断是否超出显示范围
        protected bool IsOutRange(float pos)
        {
            Vector3 listP = m_ContentRectTrans.anchoredPosition;
            if(m_Direction == e_Direction.Vertical)
            {
                if (pos + listP.y > m_CellObjectHeight || pos + listP.y < -rectTrans.rect.height)
                {
                    return true;
                }
            }
            else
            {
                if (pos + listP.x < -m_CellObjectWidth || pos + listP.x > rectTrans.rect.width)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsOutRange(RectTransform contentRect,Vector2 pos,Vector2 childSize, RectTransform viewportRect)
        {
            Vector3 listP = contentRect.anchoredPosition;
            if (m_Direction == e_Direction.Vertical)
            {
                if (pos.y + listP.y > childSize.y || pos.y + listP.y < -viewportRect.rect.height)
                {
                    return true;
                }
            }
            else
            {
                if (pos.x + listP.x < -childSize.x || pos.x + listP.x > viewportRect.rect.width)
                {
                    return true;
                }
            }
            return false;
        }

        //对象池 机制  (存入， 取出) cell
        protected Stack<GameObject> poolsObj = new Stack<GameObject>();
        //取出 cell
        protected virtual GameObject GetPoolsObj()
        {
            GameObject cell = null;
            if (poolsObj.Count > 0)
            {
                cell = poolsObj.Pop();
            }

            if (cell == null)
            {
                cell = Instantiate(m_CellGameObject) as GameObject;
            }
            cell.transform.SetParent(m_Content.transform,false);
            cell.transform.localScale = Vector3.one;
            //cell.transform.localPosition = Vector3.zero;
            UIUtils.SetActive(cell, true);
            
            return cell;
        }
        //存入 cell
        protected virtual void SetPoolsObj(GameObject cell)
        {
            if (cell != null)
            {
                poolsObj.Push(cell);
                UIUtils.SetActive(cell, false);
            }
        }

        public Transform ptrRowBg;
        public GameObject itemRowBg;

        //对象池 机制  (存入， 取出) cell
        protected Stack<GameObject> poolsRowBg = new Stack<GameObject>();
        //取出 cell
        protected virtual GameObject GetPoolsRowBg()
        {
            if (ptrRowBg == null)
            {
                return null;
            }
            GameObject cell = null;
            if (poolsRowBg.Count > 0)
            {
                cell = poolsRowBg.Pop();
            }
            if (cell == null)
            {
                cell = Instantiate(itemRowBg) as GameObject;
            }
            cell.transform.SetParent(ptrRowBg, false);
            cell.transform.localScale = Vector3.one;
            //cell.transform.localPosition = Vector3.zero;
            UIUtils.SetActive(cell, true);

            return cell;
        }
        //存入 cell
        protected virtual void SetPoolsRowBg(GameObject cell)
        {
            if (cell != null)
            {
                poolsRowBg.Push(cell);
                UIUtils.SetActive(cell, false);
            }
        }


        //回调
        protected void Func(Action<GameObject, int> func, CellInfo cellInfo, bool isUpdate = false)
        {
            //int num = int.Parse(selectObject.name) + 1;
            if (func != null)
            {
                func(cellInfo.obj, cellInfo.index);
            }
        }

        protected void Func(Action<GameObject, int> func, GameObject selectObject, bool isUpdate = false)
        {
            int num = int.Parse(selectObject.name) + 1;
            if (func != null)
            {
                func(selectObject, num);
            }
        }

        public void DisposeAll()
        {
            if (m_FuncCallBackFunc != null)
            {
                m_FuncCallBackFunc = null;
            }
            if (m_FuncOnClickCallBack != null)
            {
                m_FuncOnClickCallBack = null;
            }
        }

        protected void OnDestroy()
        {
            DisposeAll();
        }

        public virtual void OnClickCell(GameObject cell) { }

        //-> ExpandCircularScrollView 函数
        public virtual void OnClickExpand(int index) { }

        //-> FlipCircularScrollView 函数
        public virtual void SetToPageIndex(int index) { }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {

        }

        public void OnDrag(PointerEventData eventData)
        {
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {

        }

        protected void OnDragListener(Vector2 value)
        {
            float normalizedPos = m_Direction == e_Direction.Vertical ? m_ScrollRect.verticalNormalizedPosition : m_ScrollRect.horizontalNormalizedPosition;

            if(m_Direction == e_Direction.Vertical)
            {
                if (m_ContentHeight - rectTrans.rect.height < 10)
                {
                    UIUtils.SetActive(m_PointingFirstArrow, false);
                    UIUtils.SetActive(m_PointingEndArrow, false);
                    return;
                }
            }
            else
            {
                if (m_ContentWidth - rectTrans.rect.width < 10)
                {
                    UIUtils.SetActive(m_PointingFirstArrow, false);
                    UIUtils.SetActive(m_PointingEndArrow, false);
                    return;
                }
            }

            if (normalizedPos >= 0.9)
            {
                UIUtils.SetActive(m_PointingFirstArrow, false);
                UIUtils.SetActive(m_PointingEndArrow, true);
            }
            else if (normalizedPos <= 0.1)
            {
                UIUtils.SetActive(m_PointingFirstArrow, true);
                UIUtils.SetActive(m_PointingEndArrow, false);
            }
            else
            {
                UIUtils.SetActive(m_PointingFirstArrow, true);
                UIUtils.SetActive(m_PointingEndArrow, true);
            }

        }
    }
}

