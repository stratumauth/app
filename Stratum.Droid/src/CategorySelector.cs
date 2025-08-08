// Copyright (C) 2025 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Newtonsoft.Json;

namespace Stratum.Droid
{
    public class CategorySelector : IEquatable<CategorySelector>
    {
        public string CategoryId { get; }
        public MetaCategory MetaCategory { get; }
      
        [JsonConstructor]
        private CategorySelector(string categoryId, MetaCategory metaCategory)
        {
            CategoryId = categoryId;
            MetaCategory = metaCategory;
        }
        
        private CategorySelector(string id)
        {
            CategoryId = id;
            MetaCategory = MetaCategory.None;
        }

        private CategorySelector(MetaCategory metaCategory)
        {
            CategoryId = null;
            MetaCategory = metaCategory;
        }

        public static CategorySelector Of(MetaCategory metaCategory)
        {
            return new CategorySelector(metaCategory);
        }

        public static CategorySelector Of(string id)
        {
            ArgumentNullException.ThrowIfNull(id);
            return new CategorySelector(id);
        }
        
        public bool Equals(CategorySelector other)
        {
            return other != null && CategoryId == other.CategoryId && MetaCategory == other.MetaCategory;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CategoryId, (int)MetaCategory);
        }
    }
}