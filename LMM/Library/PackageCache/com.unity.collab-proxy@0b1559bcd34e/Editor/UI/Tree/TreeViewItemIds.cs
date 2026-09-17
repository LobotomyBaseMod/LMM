using System.Collections.Generic;
using System.Linq;

namespace Unity.PlasticSCM.Editor.UI.Tree
{
    internal class TreeViewItemIds<T>
    {
        internal TreeViewItemIds()
        {
            mKeysById = new Dictionary<string, int>();
            mItemsById = new Dictionary<int, T>();
        }

        internal bool TryGetItemIdByKey(string key, out int itemId)
        {
            return mKeysById.TryGetValue(key, out itemId);
        }

        internal bool TryGetItemById(int itemId, out T item)
        {
            return mItemsById.TryGetValue(itemId, out item);
        }

        internal int AddItemIdByKey(string key)
        {
            int itemId = GetNextItemId();

            mKeysById.Add(key, itemId);

            return itemId;
        }

        internal void AddItemById(int itemId, T item)
        {
            mItemsById[itemId] = item;
        }

        internal void ClearItems()
        {
            mItemsById.Clear();
        }

        int GetNextItemId()
        {
            return mKeysById.Count + 1;
        }

        readonly Dictionary<string, int> mKeysById;
        readonly Dictionary<int, T> mItemsById;
    }

    internal class TreeViewItemIds<C, I>
    {
        internal void Clear()
        {
            mCacheByCategories.Clear();
            mCacheByInfo.Clear();
        }

        internal List<int> GetCategoryIds()
        {
            return new List<int>(mCacheByCategories.Values);
        }

        internal List<KeyValuePair<C, int>> GetCategoryItems()
        {
            return mCacheByCategories.ToList();
        }

        internal List<KeyValuePair<I, int>> GetInfoItems()
        {
            return mCacheByInfo.ToList();
        }

        internal bool TryGetCategoryItemId(C category, out int itemId)
        {
            return mCacheByCategories.TryGetValue(category, out itemId);
        }

        internal bool TryGetInfoItemId(I info, out int itemId)
        {
            return mCacheByInfo.TryGetValue(info, out itemId);
        }

        internal int AddCategoryItem(C category)
        {
            int itemId = GetNextItemId();

            mCacheByCategories.Add(category, itemId);

            return itemId;
        }

        internal int AddInfoItem(I info)
        {
            int itemId = GetNextItemId();

            mCacheByInfo.Add(info, itemId);

            return itemId;
        }

        int GetNextItemId()
        {
            return mCacheByCategories.Count
                + mCacheByInfo.Count
                + 1;
        }

        readonly Dictionary<C, int> mCacheByCategories = new Dictionary<C, int>();
        readonly Dictionary<I, int> mCacheByInfo = new Dictionary<I, int>();
    }
}
