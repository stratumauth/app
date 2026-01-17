// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using Stratum.Core.Entity;
using Stratum.Core.Persistence;

namespace Stratum.Desktop.Persistence
{
    public class CategoryRepository : AsyncRepository<Category, string>, ICategoryRepository
    {
        public CategoryRepository(Database database) : base(database)
        {
        }
    }
}
