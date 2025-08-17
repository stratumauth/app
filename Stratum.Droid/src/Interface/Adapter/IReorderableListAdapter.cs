// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

namespace Stratum.Droid.Interface.Adapter
{
    public interface IReorderableListAdapter
    {
        void MoveItemView(int oldPosition, int newPosition);
        void OnMovementStarted();
        void OnMovementFinished(bool orderChanged);
    }
}