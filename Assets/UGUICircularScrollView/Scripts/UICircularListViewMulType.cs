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


    /// <summary>
    /// （ps:CellItem、Content 的Anchors 需为Top-Left Pivot为（0，1））
    /// </summary>

    [RequireComponent(typeof(ScrollRect))]
    public class UICircularListViewMulType : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        GameObject m_PointingFirstArrow;
        GameObject m_PointingEndArrow;
        bool m_IsShowArrow = false;

        public e_Direction m_Direction = e_Direction.Vertical;


        int m_Row = 1;
        [SerializeField]
        Vector2[] m_Spacing; //间距
        [SerializeField]
        GameObject[] m_CellGameObject; //指定的cell
        /// <summary>
        /// 刷新item
        /// </summary>
        protected Action<MCellInfo, int> m_CallRefreshItem;

        public Action mOnScollCall;


        protected RectTransform viewRectTrans;
        protected float m_PlaneWidth;
        protected float m_PlaneHeight;

        protected float m_ContentWidth;
        protected float m_ContentHeight;

        //protected float m_CellObjectWidth;
        //protected float m_CellObjectHeight;

        protected GameObject m_Content;
        protected RectTransform m_ContentRectTrans;

        private bool m_isInited = false;

        //记录 物体的坐标 和 物体 
        public class MCellInfo
        {
            public int index;
            public int viewType;
            public Vector3 pos;
            public GameObject obj;
        };

        protected List<MCellInfo> m_CellInfos;

        protected ScrollRect m_ScrollRect;

        protected bool m_IsClearList = false; //是否清空列表

        public void Init(Action<MCellInfo, int> callRefreshItem)
        {
            DisposeAll();
            if (m_isInited)
                return;
            m_CallRefreshItem = callRefreshItem;
            m_ScrollRect = this.GetComponent<ScrollRect>();
            m_Content = m_ScrollRect.content.gameObject;
            initPools();
            m_ScrollRect = this.GetComponent<ScrollRect>();

            m_ScrollRect.onValueChanged.RemoveAllListeners();
            //添加滑动事件
            m_ScrollRect.onValueChanged.AddListener(delegate (Vector2 value) { ScrollRectListener(value); });

            if (m_PointingFirstArrow != null || m_PointingEndArrow != null)
            {
                m_ScrollRect.onValueChanged.AddListener(delegate (Vector2 value) { OnDragListener(value); });
                OnDragListener(Vector2.zero);
            }
            updatePlaneInfo();

            //记录 Content 信息
            m_ContentRectTrans = m_Content.GetComponent<RectTransform>();
            Rect contentRect = m_ContentRectTrans.rect;
            m_ContentHeight = contentRect.height;
            m_ContentWidth = contentRect.width;
            m_ContentRectTrans.pivot = new Vector2(0f, 1f);
            CheckAnchor(m_ContentRectTrans);

            m_isInited = true;
        }

        void updatePlaneInfo()
        {
            //记录 Plane 信息
            viewRectTrans = m_ScrollRect.viewport.GetComponent<RectTransform>();
            Rect planeRect = viewRectTrans.rect;
            m_PlaneHeight = planeRect.height;
            m_PlaneWidth = planeRect.width;
        }

        public void scollToIndex(int index)
        {
            //index = Mathf.CeilToInt(index / m_Row);
            if (m_CellInfos == null || m_CellInfos.Count <= 0)
            {
                return;
            }
            float value = 0;
            int viewSize = 0;
            float scollSize = 0;
            Rect planeRect = viewRectTrans.rect;
            if (m_Direction == e_Direction.Vertical)
            {
                scollSize = contentSize - planeRect.height;
                value = (scollSize + m_CellInfos[index].pos.y) / scollSize;
                value = value < 0 ? 0 : value;
                //Debug.Log("scollToIndex value="+ value);
                m_ScrollRect.verticalNormalizedPosition = value;
            }
            else
            {
                scollSize = contentSize - planeRect.width;
                value = m_CellInfos[index].pos.x / scollSize;
                value = value > 1 ? 1 : value;
                m_ScrollRect.horizontalNormalizedPosition = value;
            }
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
            }
        }



        public int getCurFirstViewIndex()
        {
            return minViewIndex;
        }


        //public Vector2 topPadding = Vector2.zero;
        Vector2[] m_cellsSize;
        float contentSize;
        void genPos(List<int> types)
        {
            if (m_CellInfos == null)
            {
                m_CellInfos = new List<MCellInfo>();
            }

            List<MCellInfo> tmpList = new List<MCellInfo>();
            int showNum = types.Count;
            int preNum = m_CellInfos.Count;
            contentSize = 0;
            for (int i = 0; i < showNum; ++i)
            {
                MCellInfo info;
                if (i < preNum)
                {
                    info = m_CellInfos[i];
                    if (info.obj != null)
                    {
                        SetPoolsObj(info.obj, info.viewType);
                        info.obj = null;
                    }
                }
                else
                {
                    info = new MCellInfo();
                }
                info.viewType = types[i];
                info.index = i;
                if (m_Direction == e_Direction.Vertical)
                {
                    info.pos = new Vector3(0, -contentSize, 0);
                    contentSize += m_cellsSize[info.viewType].y;
                }
                else
                {
                    info.pos = new Vector3(contentSize, 0, 0);
                    contentSize += m_cellsSize[info.viewType].x;
                }
                tmpList.Add(info);

            }
            Rect planeRect = viewRectTrans.rect;
            if (m_Direction == e_Direction.Vertical)
            {
                if (contentSize - planeRect.height < 0)
                {
                    contentSize = planeRect.height;
                }
            }
            else
            {
                if (contentSize - planeRect.width < 0)
                {
                    contentSize = planeRect.width;
                }
            }
            m_CellInfos = tmpList;
            //Debug.Log("cellcount = " + m_CellInfos.Count) ;
        }

        public virtual void ShowList(string numStr) { }
        public virtual void ShowList(List<int> types)
        {
            updatePlaneInfo();
            genPos(types);
            scollToIndex(0);
            //RectTransform rectTrans = m_ContentRectTrans;
            //-> 计算 Content 尺寸
            if (m_Direction == e_Direction.Vertical)
            {
                m_ContentWidth = m_ContentRectTrans.sizeDelta.x;
                m_ContentHeight = contentSize;
                contentSize = contentSize < viewRectTrans.rect.width ? viewRectTrans.rect.width : contentSize;
                m_ContentRectTrans.sizeDelta = new Vector2(m_ContentWidth, m_ContentHeight);
            }
            else
            {
                m_ContentWidth = contentSize;
                m_ContentHeight = m_ContentRectTrans.sizeDelta.y;
                contentSize = contentSize < viewRectTrans.rect.width ? viewRectTrans.rect.width : contentSize;
                m_ContentRectTrans.sizeDelta = new Vector2(m_ContentWidth, m_ContentHeight);

            }
            UpdateCheck();

            OnDragListener(Vector2.zero);

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

        //实时刷新列表时用
        public virtual void UpdateList()
        {
            UpdateCheck();
        }

        //刷新某一项
        public void UpdateCell(int index)
        {
            MCellInfo cellInfo = m_CellInfos[index - 1];
            if (cellInfo.obj != null)
            {
                float rangePos = m_Direction == e_Direction.Vertical ? cellInfo.pos.y : cellInfo.pos.x;
                if (IsInRange(m_ContentRectTrans, cellInfo.pos, m_cellsSize[cellInfo.viewType], viewRectTrans))
                {
                    Func(m_CallRefreshItem, cellInfo, index);
                }
            }
        }


        public int minViewIndex = 0;

        public int maxViewIndex = 0;
        private void UpdateCheck()
        {
            if (m_CellInfos == null)
                return;
            int viewIndex = -1;
            //检查超出范围
            for (int i = 0, length = m_CellInfos.Count; i < length; i++)
            {
                MCellInfo cellInfo = m_CellInfos[i];
                GameObject obj = cellInfo.obj;
                Vector3 pos = cellInfo.pos;
                //判断是否超出显示范围
                if (!IsInRange(m_ContentRectTrans, cellInfo.pos, m_cellsSize[cellInfo.viewType], viewRectTrans))
                {
                    //把超出范围的cell 扔进 poolsObj里
                    if (obj != null)
                    {
                        SetPoolsObj(obj, cellInfo.viewType);
                        cellInfo.obj = null;
                    }
                }
                else
                {
                    if (obj == null)
                    {
                        //优先从 poolsObj中 取出 （poolsObj为空则返回 实例化的cell）
                        GameObject cell = GetPoolsObj(cellInfo.viewType);
                        cell.transform.localPosition = pos;
                        cell.gameObject.name = i.ToString() + "_" + cellInfo.viewType;
                        cellInfo.obj = cell;
                        Func(m_CallRefreshItem, cellInfo, i);
                    }
                    if (viewIndex == -1)
                    {
                        viewIndex = i;
                        minViewIndex = viewIndex;
                    }
                    else
                    {
                        maxViewIndex = i;
                    }
                }
            }
        }

        //判断是否超出显示范围
        //protected bool IsOutRange(float pos)
        //{
        //    Vector3 listP = m_ContentRectTrans.anchoredPosition;
        //    if (m_Direction == e_Direction.Vertical)
        //    {
        //        if (pos + listP.y > m_CellObjectHeight || pos + listP.y < -viewRectTrans.rect.height)
        //        {
        //            return true;
        //        }
        //    }
        //    else
        //    {
        //        if (pos + listP.x < -m_CellObjectWidth || pos + listP.x > viewRectTrans.rect.width)
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        //判断是否超出显示范围
        //protected bool IsOutRange(MCellInfo info)
        //{
        //    return IsOutRange(m_ContentRectTrans,info.pos,m_cellsSize[info.viewType],viewRectTrans);
        //}

        //判断是否超出显示范围
        public bool IsInRange(RectTransform contentRect, Vector2 pos, Vector2 childSize, RectTransform viewportRect)
        {
            Vector3 listP = contentRect.anchoredPosition;
            //Debug.Log(pos.ToString() + "_" + listP.y + " , " + listP.x); ;
            if (m_Direction == e_Direction.Vertical)
            {
                if (pos.y + listP.y < childSize.y && pos.y + listP.y > -(viewportRect.rect.height))
                {
                    return true;
                }
            }
            else
            {
                if (pos.x + listP.x > childSize.x && pos.x + listP.x < viewportRect.rect.width)
                {
                    return true;
                }
            }
            return false;
        }

        //对象池 机制  (存入， 取出) cell
        protected List<Stack<GameObject>> list_poolsObj = new List<Stack<GameObject>>();
        List<GameObject> list_obj_pool = new List<GameObject>();
        void initPools()
        {
            int len = m_CellGameObject.Length;
            m_cellsSize = new Vector2[len];
            for (int i = 0; i < len; ++i)
            {
                GameObject obj = new GameObject();
                obj.SetActive(false);
                obj.name = "pool_" + i;
                obj.transform.SetParent(m_Content.transform);
                list_poolsObj.Add(new Stack<GameObject>());
                list_obj_pool.Add(obj);
                //记录Cell信息
                GameObject cellObj = m_CellGameObject[i];
                cellObj.SetActive(false);
                RectTransform cellRectTrans = cellObj.GetComponent<RectTransform>();
                cellRectTrans.pivot = new Vector2(0f, 1f);
                CheckAnchor(cellRectTrans);
                cellRectTrans.anchoredPosition = Vector2.zero;
                m_cellsSize[i] = m_CellGameObject[i].transform.GetComponent<RectTransform>().sizeDelta + m_Spacing[i];
            }
        }


        //取出 cell
        protected GameObject GetPoolsObj(int type)
        {
            Stack<GameObject> poolsObj = list_poolsObj[type];
            GameObject cell = null;
            if (poolsObj.Count > 0)
            {
                cell = poolsObj.Pop();
            }
            else
            {
                cell = Instantiate(m_CellGameObject[type]) as GameObject;
            }
            cell.transform.SetParent(m_Content.transform);
            cell.transform.localScale = Vector3.one;
            UIUtils.SetActive(cell, true);
            return cell;
        }
        //存入 cell
        protected void SetPoolsObj(GameObject cell, int type)
        {
            //Debug.Log("SetPoolsObj:"+ cell.name);
            if (cell != null)
            {
                list_poolsObj[type].Push(cell);
                cell.transform.SetParent(list_obj_pool[type].transform);
                UIUtils.SetActive(cell, false);
            }
        }

        //回调
        protected void Func(Action<MCellInfo, int> func, MCellInfo info, int index)
        {

            if (func != null)
            {
                func(info, index);
            }
        }



        public void DisposeAll()
        {

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

            if (m_Direction == e_Direction.Vertical)
            {
                if (m_ContentHeight - viewRectTrans.rect.height < 10)
                {
                    UIUtils.SetActive(m_PointingFirstArrow, false);
                    UIUtils.SetActive(m_PointingEndArrow, false);
                    return;
                }
            }
            else
            {
                if (m_ContentWidth - viewRectTrans.rect.width < 10)
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

