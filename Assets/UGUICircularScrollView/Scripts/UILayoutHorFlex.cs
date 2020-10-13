using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 
/// </summary>
public class UILayoutHorFlex :MonoBehaviour
{
    [NonSerialized]
    public List<UIItemFlex> showItems = new List<UIItemFlex>();
    /// <summary>
    /// content：Pivot需为(0,1)
    /// </summary>
    [SerializeField]
    RectTransform content;
    int curRow = 1;
    public void Init()
    {
        if (content == null)
        {
            content = GetComponent<RectTransform>();
        }
        contentW = content.sizeDelta.x;
    }

    public int GetCurRow()
    {
        return curRow;
    }

    float contentW;
    [SerializeField]
    float cellH;
    Vector2 lastItemPos;
    [SerializeField]
    Vector2 space = new Vector2(0,0);
    Vector2 conteSize;

    /// <summary>
    /// itemRect：Pivot需为(0,1)
    /// </summary>
    /// <param name="itemRect"></param>
    public bool AddObj(UIItemFlex item,int maxLine = 0)
    {
        //item.RefreshRect(showItems.Count);
       
        RectTransform itemRect = item.trRect;
        if (curRow == 0)
        {
            curRow = 1;
        }
        itemRect.SetParent(content,false);
        //Vector2 itemWH = item.GetRectW();
        float itemW = item.GetRectW();
        float posX, posY;
        if (lastItemPos.x + itemW > contentW)
        {
            if (maxLine>0)
            {
                if(curRow == maxLine)
                {
                    return false;
                }
            }
            posX = 0;
            posY = lastItemPos.y - (cellH + space.y);
            ++curRow;
        }
        else
        {
            posX = lastItemPos.x;
            posY = lastItemPos.y;
        }
        showItems.Add(item);
        conteSize = new Vector2(contentW,cellH * curRow+(curRow-1)*space.y);
        itemRect.localPosition = new Vector3(posX,posY,0);
        lastItemPos =  new Vector2(posX+ itemW + space.x,posY);
        content.sizeDelta = conteSize;
        return true;
    }



    public void Reset()
    {
        Init();
        curRow = 0;
        lastItemPos = Vector2.zero;
        conteSize = new Vector2(contentW,cellH);
        content.sizeDelta = conteSize;
        for (int i = showItems.Count-1;i>=0;--i)
        {
            showItems[i].Remove();
        }
        showItems.Clear();
    }

}

public abstract class UIItemFlex : MonoBehaviour
{
    bool mIsInit = false;
    public delegate void DoRemove(UIItemFlex item);
    public DoRemove mCallRemove;
    int mIndex;
    public RectTransform trRect;
    //public abstract void RefreshRect(int index);

    public virtual bool Init(DoRemove call)
    {
        if (mIsInit)
        {
            return true;
        }
        if (trRect == null)
        {
            trRect = GetComponent<RectTransform>();
        }
        mCallRemove = call;
        mIsInit = true;
        return mIsInit;
    }


    public virtual void Remove()
    {
        if (mCallRemove!=null)
        {
            mCallRemove.Invoke(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //public virtual Vector2 GetRectV()
    //{
    //    return trRect.sizeDelta;
    //}

    public virtual float GetRectW()
    {
        return trRect.sizeDelta.x;
    }

    public virtual float GetRectH()
    {
        return trRect.sizeDelta.y;
    }
}