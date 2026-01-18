// Copyright (C) 2025 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Linq;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Wear.Tiles;
using AndroidX.Wear.Widget;
using Java.Lang;
using Stratum.Core;
using Stratum.Core.Generator;
using Stratum.Core.Util;
using Stratum.WearOS.Activity;
using Stratum.WearOS.Cache;
using Stratum.WearOS.Cache.View;
using Stratum.WearOS.Interface;

namespace Stratum.WearOS.Fragment
{
    public class AuthListFragment() : AndroidX.Fragment.App.Fragment(Resource.Layout.fragmentList)
    {
        private AuthenticatorView _authView;
        private CustomIconCache _customIconCache;
        
        private PreferenceWrapper _preferences;
        
        private RelativeLayout _emptyLayout;
        private WearableRecyclerView _authList;
        private AuthenticatorListAdapter _authListAdapter;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            _authView = Dependencies.Resolve<AuthenticatorView>();
            _customIconCache = Dependencies.Resolve<CustomIconCache>();
            _preferences = new PreferenceWrapper(RequireContext());
            
            _authListAdapter = new AuthenticatorListAdapter(_authView, _customIconCache, _preferences.ShowUsernames);
            _authListAdapter.ItemClicked += OnItemClicked;
            _authListAdapter.ItemLongClicked += OnItemLongClicked;
            _authListAdapter.HasStableIds = true;
            _authListAdapter.DefaultAuth = _preferences.DefaultAuth;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            
            _emptyLayout = view.FindViewById<RelativeLayout>(Resource.Id.layoutEmpty);
            
            _authList = view.FindViewById<WearableRecyclerView>(Resource.Id.list);
            _authList.EdgeItemsCenteringEnabled = true;
            _authList.HasFixedSize = true;
            _authList.SetItemViewCacheSize(12);
            _authList.SetItemAnimator(null);

            var layoutCallback = new AuthenticatorListLayoutCallback(RequireContext());
            _authList.SetLayoutManager(new WearableLinearLayoutManager(RequireContext(), layoutCallback));
            _authList.SetAdapter(_authListAdapter);
            
            CheckEmptyState();
        }

        private void CheckEmptyState()
        {
            if (!_authView.Any())
            {
                _emptyLayout.Visibility = ViewStates.Visible;
                _authList.Visibility = ViewStates.Invisible;
            }
            else
            {
                _emptyLayout.Visibility = ViewStates.Gone;
                _authList.Visibility = ViewStates.Visible;
                _authList.RequestFocus();
            }
        }

        public void NotifyChanged()
        {
            _authListAdapter?.NotifyDataSetChanged();

            if (_authList != null)
            {
                CheckEmptyState();
            }
        }
        
        private void OnItemClicked(object sender, int position)
        {
            var item = _authView.ElementAtOrDefault(position);

            if (item == null)
            {
                return;
            }

            if (item.Type.GetGenerationMethod() == GenerationMethod.Counter)
            {
                Toast.MakeText(RequireContext(), Resource.String.hotpNotSupported, ToastLength.Short).Show();
                return;
            }
            
            var bundle = new Bundle();
            bundle.PutInt("position", position);
            
            RequireActivity().SupportFragmentManager.SetFragmentResult(MainActivity.ResultItemClicked, bundle);
        }

        private void OnItemLongClicked(object sender, int position)
        {
            var item = _authView.ElementAtOrDefault(position);

            if (item == null)
            {
                return;
            }
            
            if (item.Type.GetGenerationMethod() == GenerationMethod.Counter)
            {
                Toast.MakeText(RequireContext(), Resource.String.hotpNotSupported, ToastLength.Short).Show();
                return;
            }

            var oldDefault = _preferences.DefaultAuth;
            var newDefault = HashUtil.Sha1(item.Secret);

            if (oldDefault == newDefault)
            {
                _authListAdapter.DefaultAuth = _preferences.DefaultAuth = null;
            }
            else
            {
                _authListAdapter.DefaultAuth = _preferences.DefaultAuth = newDefault;
                _authListAdapter.NotifyItemChanged(position);
            }

            if (oldDefault != null)
            {
                var oldPosition = _authView.FindIndex(a => HashUtil.Sha1(a.Secret) == oldDefault);

                if (oldPosition > -1)
                {
                    _authListAdapter.NotifyItemChanged(oldPosition);
                }
            }

            var tileClass = Class.FromType(typeof(AuthTileService));
            TileService.GetUpdater(RequireContext()).RequestUpdate(tileClass);
        }
    }
}