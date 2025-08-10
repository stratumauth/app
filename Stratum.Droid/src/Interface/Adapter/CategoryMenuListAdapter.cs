// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.Content;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using Stratum.Droid.Interface.ViewHolder;
using Stratum.Droid.Persistence.View;

namespace Stratum.Droid.Interface.Adapter
{
    public class CategoryMenuListAdapter : RecyclerView.Adapter
    {
        private readonly Context _context;
        private readonly ICategoryView _categoryView;
        private readonly bool _showUncategorisedCategory;

        public CategoryMenuListAdapter(Context context, ICategoryView categoryView, bool showUncategorisedCategory)
        {
            _context = context;
            _categoryView = categoryView;
            _showUncategorisedCategory = showUncategorisedCategory;
            SelectedPosition = 0;
        }

        public int SelectedPosition { get; set; }

        private int MetaCategoryCount => _showUncategorisedCategory ? 2 : 1;
        public override int ItemCount => _categoryView.Count + MetaCategoryCount;
        
        public event EventHandler<CategorySelector> CategorySelected;

        public override long GetItemId(int position)
        {
            return position < MetaCategoryCount
                ? -MetaCategoryCount
                : _categoryView[position - MetaCategoryCount].GetHashCode();
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var holder = (CategoriesListHolder) viewHolder;

            var name = position switch
            {
                0 => _context.Resources.GetString(Resource.String.categoryAll),
                1 when MetaCategoryCount > 1 => _context.Resources.GetString(Resource.String.categoryUncategorised),
                _ => _categoryView[position - MetaCategoryCount].Name
            };

            holder.Name.Text = name;
            holder.ItemView.Selected = position == SelectedPosition;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.listItemCategory, parent, false);

            var holder = new CategoriesListHolder(itemView);
            holder.Clicked += (_, position) =>
            {
                NotifyItemChanged(SelectedPosition);
                SelectedPosition = position;
                NotifyItemChanged(position);
                
                var categorySelector = position switch
                {
                    0 => CategorySelector.Of(MetaCategory.All),
                    1 when MetaCategoryCount > 1 => CategorySelector.Of(MetaCategory.Uncategorised),
                    _ => CategorySelector.Of(_categoryView[position - MetaCategoryCount].Id)
                };

                CategorySelected?.Invoke(this, categorySelector);
            };

            return holder;
        }

        public int PositionOf(CategorySelector selector)
        {
            if (selector.IsMetaCategory(out var metaCategory))
            {
                return metaCategory switch
                {
                    MetaCategory.All => 0,
                    MetaCategory.Uncategorised => 1
                };
            }

            if (selector.IsCategory(out var categoryId))
            {
                return _categoryView.IndexOf(categoryId) + MetaCategoryCount;
            }

            return -1;
        }
    }
}