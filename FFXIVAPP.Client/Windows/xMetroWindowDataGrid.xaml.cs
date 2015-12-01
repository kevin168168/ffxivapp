﻿// FFXIVAPP.Client
// FFXIVAPP & Related Plugins/Modules
// Copyright © 2007 - 2015 Ryan Wilson - All Rights Reserved
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.Windows;
using FFXIVAPP.Client.Properties;
using FFXIVAPP.Common.Helpers;
using MahApps.Metro.Controls;

namespace FFXIVAPP.Client.Windows
{
    /// <summary>
    ///     Interaction logic for xMetroWindowDataGrid.xaml
    /// </summary>
    public partial class xMetroWindowDataGrid
    {
        public xMetroWindowDataGrid()
        {
            InitializeComponent();
        }

        private void XMetroWindowDataGrid_OnLoaded(object sender, RoutedEventArgs e)
        {
            ThemeHelper.ChangeTheme(Settings.Default.Theme, new List<MetroWindow>
            {
                this
            });
        }
    }
}
