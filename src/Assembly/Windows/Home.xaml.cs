﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Assembly.Helpers;
using Assembly.Helpers.Native;
using Assembly.Helpers.Net;
using Assembly.Metro.Controls.PageTemplates;
using Assembly.Metro.Controls.PageTemplates.Games;
using Assembly.Metro.Controls.PageTemplates.Tools;
using Assembly.Metro.Controls.PageTemplates.Tools.Halo4;
using Assembly.Metro.Dialogs;
using Xceed.Wpf.AvalonDock.Layout;
using Blamite.Blam.ThirdGen;
using Blamite.IO;
using Microsoft.Win32;
using XboxChaos.Models;
using XBDMCommunicator;
using System.Windows.Controls;

namespace Assembly.Windows
{
	/// <summary>
	///     Interaction logic for Home.xaml
	/// </summary>
	public partial class Home
	{
		public Home()
		{
			InitializeComponent();

			DwmDropShadow.DropShadowToWindow(this);

			UpdateTitleText("");
			UpdateStatusText("Ready...");

			//Window_StateChanged(null, null);
			ClearTabs();

			if (App.AssemblyStorage.AssemblySettings.StartpageShowOnLoad)
				AddTabModule(TabGenre.StartPage);

			// Do sidebar Loading stuff
			//SwitchXBDMSidebarLocation(App.AssemblyStorage.AssemblySettings.applicationXBDMSidebarLocation);
			//XBDMSidebarTimerEvent();

			// Set width/height/state from last session
			if (!double.IsNaN(App.AssemblyStorage.AssemblySettings.ApplicationSizeHeight) &&
			    App.AssemblyStorage.AssemblySettings.ApplicationSizeHeight > MinHeight)
				Height = App.AssemblyStorage.AssemblySettings.ApplicationSizeHeight;
			if (!double.IsNaN(App.AssemblyStorage.AssemblySettings.ApplicationSizeWidth) &&
			    App.AssemblyStorage.AssemblySettings.ApplicationSizeWidth > MinWidth)
				Width = App.AssemblyStorage.AssemblySettings.ApplicationSizeWidth;

			WindowState = App.AssemblyStorage.AssemblySettings.ApplicationSizeMaximize
				? WindowState.Maximized
				: WindowState.Normal;
			Window_StateChanged(null, null);

			AllowDrop = true;
			App.AssemblyStorage.AssemblySettings.HomeWindow = this;
		}

		protected override async void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			IntPtr handle = (new WindowInteropHelper(this)).Handle;
			HwndSource hwndSource = HwndSource.FromHwnd(handle);
			if (hwndSource != null)
				hwndSource.AddHook(WindowProc);

			ProcessCommandLineArgs(Environment.GetCommandLineArgs());

			if (!App.AssemblyStorage.AssemblySettings.ShownCheatingDialog)
			{
				ShowCheatingDialog();
				App.AssemblyStorage.AssemblySettings.ShownCheatingDialog = true;
			}

			if (App.AssemblyStorage.AssemblySettings.ApplicationUpdateOnStartup)
				await CheckForUpdates();
		}

		private async Task CheckForUpdates()
		{
			// Grab JSON Update package from the server
			try
			{
				var result = await Updater.GetBranchInfo();
				if (result == null)
					return;
				if (Updater.UpdateAvailable(result))
					MetroUpdateDialog.Show(result, true);
			}
			catch
			{
			}
		}

		private void LayoutRoot_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var activeContent = ((LayoutRoot)sender).ActiveContent;

			if (activeContent == null)
			{
				UpdateTitleText("");
				return;
			}

			if (e.PropertyName == "ActiveContent")
			{
				if (activeContent.Content != null)
					UpdateTitleText(activeContent.Title.Replace("__", "_")
						.Replace(".mapinfo", "").Replace(".map", "").Replace(".campaign", "").Replace(".blf", ""));

				if (activeContent != null && activeContent.Title == "Start Page")
					((StartPage)activeContent.Content).UpdateRecents();

				if (activeContent != null && activeContent.Title == "Imgur History")
					((ImgurHistoryPage)activeContent.Content).UpdateHistory();
			}
		}

		// File
		private void menuOpenCacheFile_Click(object sender, RoutedEventArgs e)
		{
			OpenContentFile(ContentTypes.Map);
		}

		private void menuOpenCacheInfomation_Click(object sender, RoutedEventArgs e)
		{
			OpenContentFile(ContentTypes.MapInfo);
		}

		private void menuOpenCacheImage_Click(object sender, RoutedEventArgs e)
		{
			OpenContentFile(ContentTypes.MapImage);
		}

		private void menuOpenCampaign_Click(object sender, RoutedEventArgs e)
		{
			OpenContentFile(ContentTypes.Campaign);
		}

		// Edit
		private void menuOpenSettings_Click(object sender, EventArgs e)
		{
			AddTabModule(TabGenre.Settings);
		}

		// Tools
		private void menuToolHalo4VoxelConverter_Click(object sender, RoutedEventArgs e)
		{
			AddTabModule(TabGenre.VoxelConverter);
		}

		// View
		private void menuViewStartPage_Click(object sender, RoutedEventArgs e)
		{
			AddTabModule(TabGenre.StartPage);
		}

		private void menuViewImgurHistoryPage_Click(object sender, RoutedEventArgs e)
		{
			AddTabModule(TabGenre.ImgurHistory);
		}

		private void menuPatches_Click(object sender, RoutedEventArgs e)
		{
			AddPatchTabModule();
		}

		private void menuPostGenerator_Click(object sender, RoutedEventArgs e)
		{
			AddTabModule(TabGenre.PostGenerator, false);
		}

		private void menuPluginGeneration_Click(object sender, RoutedEventArgs e)
		{
			AddTabModule(TabGenre.PluginGenerator);
		}

		private void menuPluginConverter_Click(object sender, RoutedEventArgs e)
		{
			AddTabModule(TabGenre.PluginConverter);
		}

		//xbdm
		private void takeScreenshot(IXbdm deviceOrDeviceCollection)
		{
            new Thread(new ThreadStart(delegate
            {
                this.Dispatcher.Invoke(delegate
                    {
                        menuScreenshot.IsEnabled = false;
                    });

                bool allSuccessful = true;
				if (deviceOrDeviceCollection is XbdmDeviceCollection)
                {
					XbdmDeviceCollection deviceCollection = (XbdmDeviceCollection)deviceOrDeviceCollection;

                    foreach (XbdmDevice device in deviceCollection.XbdmDevices)
                        if (!createScreenShotTab(device))
                            allSuccessful = false;
                }
                else
					allSuccessful = createScreenShotTab((XbdmDevice)deviceOrDeviceCollection);

                this.Dispatcher.Invoke(delegate
                {
                    menuScreenshot.IsEnabled = true;

                    if (!allSuccessful)
                        MetroMessageBox.Show("Not Connected", "One or more devices failed to take retrieve a screenshot.");
                });

            })).Start();
		}

        private bool createScreenShotTab(XbdmDevice device)
        {
            bool success = true;
            var screenshotFileName = Path.GetTempFileName();
            try
            {
                if (device.GetScreenshot(screenshotFileName))
                {
                    this.Dispatcher.Invoke(delegate
                    {
                        App.AssemblyStorage.AssemblySettings.HomeWindow.AddScrenTabModule(screenshotFileName);
                    });
                }
                else
                    success = false;
            }
            finally
            {
                File.Delete(screenshotFileName);
            }

            return success;
        }

		// Help
		private void menuHelpAbout_Click(object sender, RoutedEventArgs e)
		{
			MetroAbout.Show();
		}

		private async void menuHelpUpdater_Click(object sender, RoutedEventArgs e)
		{
			await Updater.BeginUpdateProcess();
		}

		// Goodbye Sweet Evelyn
		private void menuCloseApplication_Click(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		private void ShowCheatingDialog()
		{
			MetroMessageBox.Show("Assembly",
				"Assembly is not a cheating tool. While you will never be prevented from using it to give yourself an unfair advantage on Xbox Live, do not expect to receive help if you ask how to do so. Discussion of cheating on Xbox Chaos will immediately result in a permanent ban from the website.\n\nThis dialog will only show once.");
		}

		#region Waste of Space, idk man

		private void Home_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			_0xabad1dea.TragicSans.KeyDown(e.Key);
		}

		private void btnIWff_Click(object sender, RoutedEventArgs e)
		{
			_0xabad1dea.IWff.CleanUp();
		}

		private void Window_PreviewDrop_1(object sender, DragEventArgs e)
		{
		}

		#endregion

		#region More WPF Annoyance

		private void ResizeDrop_DragDelta(object sender, DragDeltaEventArgs e)
		{
			double yadjust = Height + e.VerticalChange;
			double xadjust = Width + e.HorizontalChange;

			if (xadjust > MinWidth)
				Width = xadjust;
			if (yadjust > MinHeight)
				Height = yadjust;
		}

		private void ResizeRight_DragDelta(object sender, DragDeltaEventArgs e)
		{
			double xadjust = Width + e.HorizontalChange;

			if (xadjust > MinWidth)
				Width = xadjust;
		}

		private void ResizeBottom_DragDelta(object sender, DragDeltaEventArgs e)
		{
			double yadjust = Height + e.VerticalChange;

			if (yadjust > MinHeight)
				Height = yadjust;
		}

		private void Window_StateChanged(object sender, EventArgs e)
		{
			switch (WindowState)
			{
				case WindowState.Normal:
					borderFrame.BorderThickness = new Thickness(1, 1, 1, 23);
					btnActionRestore.Visibility = Visibility.Collapsed;
					btnActionMaxamize.Visibility =
						ResizeDropVector.Visibility =
							ResizeDrop.Visibility = ResizeRight.Visibility = ResizeBottom.Visibility = Visibility.Visible;
					break;
				case WindowState.Maximized:
					borderFrame.BorderThickness = new Thickness(0, 0, 0, 23);
					btnActionRestore.Visibility = Visibility.Visible;
					btnActionMaxamize.Visibility =
						ResizeDropVector.Visibility =
							ResizeDrop.Visibility = ResizeRight.Visibility = ResizeBottom.Visibility = Visibility.Collapsed;
					break;
			}
			/*
			 * ResizeDropVector
			 * ResizeDrop
			 * ResizeRight
			 * ResizeBottom
			 */
		}

		private void btnActionSupport_Click(object sender, RoutedEventArgs e)
		{
			// Load support page?
		}

		private void btnActionMinimize_Click(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState.Minimized;
		}

		private void btnActionRestore_Click(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState.Normal;
		}

		private void btnActionMaxamize_Click(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState.Maximized;
		}

		private void btnActionClose_Click(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		#region Maximize Workspace Workarounds

		private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			switch (msg)
			{
				case 0x0024:
					WmGetMinMaxInfo(hwnd, lParam);
					handled = true;
					break;
			}
			return IntPtr.Zero;
		}

		private static void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
		{
			var mmi = (Monitor_Workarea.MINMAXINFO) Marshal.PtrToStructure(lParam, typeof (Monitor_Workarea.MINMAXINFO));

			// Adjust the maximized size and position to fit the work area of the correct monitor
			const int monitorDefaulttonearest = 0x00000002;
			IntPtr monitor = Monitor_Workarea.MonitorFromWindow(hwnd, monitorDefaulttonearest);

			if (monitor != IntPtr.Zero)
			{
				var monitorInfo = new Monitor_Workarea.MONITORINFO();
				Monitor_Workarea.GetMonitorInfo(monitor, monitorInfo);
				Monitor_Workarea.RECT rcWorkArea = monitorInfo.rcWork;
				Monitor_Workarea.RECT rcMonitorArea = monitorInfo.rcMonitor;
				mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left);
				mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top);
				mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left);
				mmi.ptMaxSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top);

				/*
				mmi.ptMaxPosition.x = Math.Abs(scrn.Bounds.Left - scrn.WorkingArea.Left);
				mmi.ptMaxPosition.y = Math.Abs(scrn.Bounds.Top - scrn.WorkingArea.Top);
				mmi.ptMaxSize.x = Math.Abs(scrn.Bounds.Right - scrn.WorkingArea.Left);
				mmi.ptMaxSize.y = Math.Abs(scrn.Bounds.Bottom - scrn.WorkingArea.Top);
				*/
			}

			Marshal.StructureToPtr(mmi, lParam, true);
		}

		#endregion

		#endregion

		#region Content Management

		public enum ContentTypes
		{
			Map,
			MapInfo,
			MapImage,
			Campaign
		}

		private readonly Dictionary<ContentTypes, ContentFileHandler> _contentFileHandlers = new Dictionary
			<ContentTypes, ContentFileHandler>
		{
			{
				ContentTypes.Map, new ContentFileHandler(
					"Assembly - Open Blam Cache File",
					"Blam Cache File (*.map)|*.map",
					(home, file) => home.AddCacheTabModule(file))
			},
			{
				ContentTypes.MapImage, new ContentFileHandler(
					"Assembly - Open Blam Map Image File",
					"Blam Map Image File (*.blf)|*.blf",
					(home, file) => home.AddImageTabModule(file))
			},
			{
				ContentTypes.MapInfo, new ContentFileHandler(
					"Assembly - Open Blam Map Info File",
					"Blam Map Info File (*.mapinfo)|*.mapinfo",
					(home, file) => home.AddInfooTabModule(file))
			},
			{
				ContentTypes.Campaign, new ContentFileHandler(
					"Assembly - Open Blam Campaign File",
					"Blam Campaign File (*.campaign)|*.campaign",
					(home, file) => home.AddCampaignTabModule(file))
			},
		};

		/// <summary>
		///     Open a new Blam Engine File
		/// </summary>
		/// <param name="contentType">Type of content to open</param>
		public void OpenContentFile(ContentTypes contentType)
		{
			ContentFileHandler handler;
			if (!_contentFileHandlers.TryGetValue(contentType, out handler)) return;

			var ofd = new OpenFileDialog
			{
				Title = handler.Title,
				Filter = handler.Filter,
				Multiselect = handler.AllowMultipleFiles,
			};

			if (!(bool) ofd.ShowDialog(this)) return;

			if (handler.AllowMultipleFiles)
				foreach (string file in ofd.FileNames)
					handler.FileHandler(this, file);
			else
				handler.FileHandler(this, ofd.FileName);
		}

		private class ContentFileHandler
		{
			public readonly Action<Home, string> FileHandler;

			public ContentFileHandler(string title, string filter, Action<Home, string> handler, bool allowMultipleFiles = true)
			{
				Title = title;
				Filter = filter;
				AllowMultipleFiles = allowMultipleFiles;
				FileHandler = handler;
			}

			public string Title { get; private set; }
			public string Filter { get; private set; }
			public bool AllowMultipleFiles { get; private set; }
		};

		#endregion

		#region Tab Manager

		public enum TabGenre
		{
			StartPage,
			Settings,
			PluginGenerator,
			Welcome,
			PluginConverter,
			ImgurHistory,

			MemoryManager,
			VoxelConverter,
			PostGenerator
		}

		public void ExternalTabClose(TabGenre tabGenre)
		{
			string tabHeader = "";
			switch (tabGenre)
			{
				case TabGenre.StartPage:
					tabHeader = "Start Page";
					break;
				case TabGenre.Settings:
					tabHeader = "Settings";
					break;
			}

			LayoutDocument toRemove = null;
			foreach (LayoutContent tab in documentManager.Children.Where(tab => tab.Title == tabHeader && tab is LayoutDocument))
				toRemove = (LayoutDocument) tab;

			if (toRemove != null)
				documentManager.Children.Remove(toRemove);
		}

		public void ExternalTabClose(LayoutDocument tab)
		{
			documentManager.Children.Remove(tab);

			if (documentManager.Children.Count > 0)
				documentManager.SelectedContentIndex = documentManager.Children.Count - 1;
		}

		public void ClearTabs()
		{
			documentManager.Children.Clear();
		}

		/// <summary>
		///     Add a new Blam Cache Editor Container
		/// </summary>
		/// <param name="cacheLocation">Path to the Blam Cache File</param>
		public void AddCacheTabModule(string cacheLocation)
		{
			// Check Map isn't already open
			foreach (LayoutContent tab in documentManager.Children.Where(tab => tab.ContentId == cacheLocation))
			{
				documentManager.SelectedContentIndex = documentManager.IndexOfChild(tab);
				return;
			}

			var newCacheTab = new LayoutDocument
			{
				ContentId = cacheLocation,
				Title = "",
				ToolTip = cacheLocation
			};
			newCacheTab.Content = new HaloMap(cacheLocation, newCacheTab, App.AssemblyStorage.AssemblySettings.HalomapTagSort);
			documentManager.Children.Add(newCacheTab);
			documentManager.SelectedContentIndex = documentManager.IndexOfChild(newCacheTab);
		}

		/// <summary>
		///     Add a new XBox Screenshot Editor Container
		/// </summary>
		/// <param name="tempImageLocation">Path to the temporary location of the image</param>
		public void AddScrenTabModule(string tempImageLocation)
		{
			var newScreenshotTab = new LayoutDocument
			{
				ContentId = tempImageLocation,
				Title = "Screenshot",
				ToolTip = tempImageLocation
			};
			newScreenshotTab.Content = new HaloScreenshot(tempImageLocation, newScreenshotTab);
			documentManager.Children.Add(newScreenshotTab);
			documentManager.SelectedContentIndex = documentManager.IndexOfChild(newScreenshotTab);
		}

		/// <summary>
		///     Add a new BLF Editor Container
		/// </summary>
		/// <param name="imageLocation">Path to the BLF file</param>
		public void AddImageTabModule(string imageLocation)
		{
			// Check File isn't already open
			foreach (LayoutContent tab in documentManager.Children.Where(tab => tab.ContentId == imageLocation))
			{
				documentManager.SelectedContentIndex = documentManager.IndexOfChild(tab);
				return;
			}

			var newMapImageTab = new LayoutDocument
			{
				ContentId = imageLocation,
				Title = "Image",
				ToolTip = imageLocation
			};
			newMapImageTab.Content = new HaloImage(imageLocation, newMapImageTab);
			documentManager.Children.Add(newMapImageTab);
			documentManager.SelectedContentIndex = documentManager.IndexOfChild(newMapImageTab);
		}

		/// <summary>
		///     Add a new MapInfo Editor Container
		/// </summary>
		/// <param name="infooLocation">Path to the MapInfo file</param>
		public void AddInfooTabModule(string infooLocation)
		{
			// Check File isn't already open
			foreach (LayoutContent tab in documentManager.Children.Where(tab => tab.ContentId == infooLocation))
			{
				documentManager.SelectedContentIndex = documentManager.IndexOfChild(tab);
				return;
			}

			var newMapInfoTab = new LayoutDocument
			{
				ContentId = infooLocation,
				Title = "Info",
				ToolTip = infooLocation
			};
			newMapInfoTab.Content = new HaloInfo(infooLocation, newMapInfoTab);
			documentManager.Children.Add(newMapInfoTab);
			documentManager.SelectedContentIndex = documentManager.IndexOfChild(newMapInfoTab);
		}

		/// <summary>
		///     Add a new Campaign Editor Container
		/// </summary>
		/// <param name="campiagnLocation">Path to the Campaign file</param>
		public void AddCampaignTabModule(string campiagnLocation)
		{
			// Check File isn't already open
			foreach (LayoutContent tab in documentManager.Children.Where(tab => tab.ContentId == campiagnLocation))
			{
				documentManager.SelectedContentIndex = documentManager.IndexOfChild(tab);
				return;
			}

			var newCampaignTab = new LayoutDocument
			{
				ContentId = campiagnLocation,
				Title = "Campaign",
				ToolTip = campiagnLocation
			};
			newCampaignTab.Content = new HaloCampaign(campiagnLocation, newCampaignTab);
			documentManager.Children.Add(newCampaignTab);
			documentManager.SelectedContentIndex = documentManager.IndexOfChild(newCampaignTab);
		}

		/// <summary>
		///     Add a new Patch Control
		/// </summary>
		/// <param name="patchLocation">Path to the Patch file</param>
		public void AddPatchTabModule(string patchLocation = null)
		{
			var newPatchTab = new LayoutDocument
			{
				Title = "Patcher",
				Content = (patchLocation != null) ? new PatchControl(patchLocation) : new PatchControl()
			};
			documentManager.Children.Add(newPatchTab);
			documentManager.SelectedContentIndex = documentManager.IndexOfChild(newPatchTab);
		}

		public void AddTabModule(TabGenre tabG, bool singleInstance = true)
		{
			var tab = new LayoutDocument();

			switch (tabG)
			{
				case TabGenre.StartPage:
					tab.Title = "Start Page";
					tab.Content = new StartPage();
					break;
				case TabGenre.Welcome:
					tab.Title = "Welcome";
					tab.Content = new WelcomePage();
					break;
				case TabGenre.Settings:
					tab.Title = "Settings";
					tab.Content = new SettingsPage();
					break;
				case TabGenre.PluginGenerator:
					tab.Title = "Plugin Generator";
					tab.Content = new HaloPluginGenerator();
					break;
				case TabGenre.PluginConverter:
					tab.Title = "Plugin Converter";
					tab.Content = new HaloPluginConverter();
					break;
				case TabGenre.ImgurHistory:
					tab.Title = "Imgur History";
					tab.Content = new ImgurHistoryPage();
					break;


				case TabGenre.MemoryManager:
					tab.Title = "Memory Manager";
					tab.Content = new MemoryManager();
					break;
				case TabGenre.VoxelConverter:
					tab.Title = "Voxel Converter";
					tab.Content = new VoxelConverter();
					break;
				case TabGenre.PostGenerator:
					tab.Title = "Post Generator";
					tab.Content = new PostGenerator();
					break;
			}

			if (singleInstance)
				foreach (LayoutContent tabb in documentManager.Children.Where(tabb => tabb.Title == tab.Title))
				{
					documentManager.SelectedContentIndex = documentManager.IndexOfChild(tabb);
					return;
				}

			documentManager.Children.Add(tab);
			documentManager.SelectedContentIndex = documentManager.IndexOfChild(tab);
		}

		#endregion

		#region Public Access Modifiers

		private readonly DispatcherTimer _statusUpdateTimer = new DispatcherTimer();

		/// <summary>
		///     Set the title text of Assembly
		/// </summary>
		/// <param name="title">Current Title, Assembly shall add the rest for you.</param>
		public void UpdateTitleText(string title)
		{
			string suffix = "Assembly";
			if (!string.IsNullOrWhiteSpace(title))
				suffix = " - " + suffix;

			Title = title + suffix;
			lblTitle.Text = title + suffix;
		}

		/// <summary>
		///     Set the status text of Assembly
		/// </summary>
		/// <param name="status">Current Status of Assembly</param>
		public void UpdateStatusText(string status)
		{
			Status.Text = status;

			_statusUpdateTimer.Stop();
			_statusUpdateTimer.Interval = new TimeSpan(0, 0, 0, 4);
			_statusUpdateTimer.Tick += statusUpdateCleaner_Clear;
			_statusUpdateTimer.Start();
		}

		private void statusUpdateCleaner_Clear(object sender, EventArgs e)
		{
			Status.Text = "Ready...";
		}

		#endregion

		#region Opacity Masking

		public int OpacityIndex;

		public void ShowMask()
		{
			OpacityIndex++;
			OpacityRect.Visibility = Visibility.Visible;
		}

		public void HideMask()
		{
			OpacityIndex--;

			if (OpacityIndex == 0)
				OpacityRect.Visibility = Visibility.Collapsed;
		}

		#endregion

		#region Drag&Drop Support

		private void HomeWindow_Drop(object sender, DragEventArgs e)
		{
			// Win7 master race lol
			string[] draggedFiles = (string[])e.Data.GetData(DataFormats.FileDrop, true);

			foreach (string file in draggedFiles)
			{
				if (file.EndsWith(".mapinfo"))
				{
					AddInfooTabModule(file);
					continue;
				}
				if (file.EndsWith(".map"))
				{
					AddCacheTabModule(file);
					continue;
				}
				if (file.EndsWith(".blf"))
				{
					AddImageTabModule(file);
					continue;
				}
				if (file.EndsWith(".campaign"))
				{
					AddCampaignTabModule(file);
					continue;
				}
				MetroMessageBox.Show("File Not Supported", "The dropped file, \"" + Path.GetFileName(file) + "\" has an invalid extension and will not be opened.");
			}
		}

		#endregion

		#region Startup

		public bool ProcessCommandLineArgs(IList<string> args)
		{
			if (args != null && args.Count > 1)
			{
				string[] commandArgs = args.Skip(1).ToArray();
				if (commandArgs[0].StartsWith("assembly://"))
					commandArgs[0] = commandArgs[0].Substring(11).Trim('/');

				// Decide what to do
				Activate();
				switch (commandArgs[0].ToLower())
				{
					case "open":
						// Determine type of file, and start it up, yo
						if (commandArgs.Length > 1)
							StartupDetermineType(commandArgs[1]);
						break;

					case "postupdate":
						var version = VersionInfo.GetUserFriendlyVersion();
						if (version != null)
						{
							MetroMessageBox.Show("Update successful!",
								"Assembly has been updated to version " + version +
								".\r\n\r\nYour old plugins and layouts were copied to a \"Backup\" folder.\r\nBe sure to merge any changes you have made on your own.");
						}
						break;

					case "update":
						// Show Update
						menuHelpUpdater_Click(null, null);
						break;

					case "about":
						// Show About
						menuHelpAbout_Click(null, null);
						break;

					case "settings":
						// Show Settings
						menuOpenSettings_Click(null, null);
						break;

					default:
						return true;
				}
			}

			return true;
		}

		private void StartupDetermineType(string path)
		{
			try
			{
				if (File.Exists(path))
				{
					// Magic Check
					string magic;
					using (var stream = new EndianReader(File.OpenRead(path), Endian.BigEndian))
						magic = stream.ReadAscii(0x04).ToLower();

					switch (magic)
					{
						case "head":
						case "daeh":
							// Map File
							AddCacheTabModule(path);
							return;

						case "asmp":
							// Patch File
							AddPatchTabModule(path);
							return;

						case "_blf":
							// BLF Container, needs more checking
							var blf = new PureBLF(path);
							blf.Close();
							if (blf.BLFChunks.Count > 2)
							{
								switch (blf.BLFChunks[1].ChunkMagic)
								{
									case "levl":
										AddInfooTabModule(path);
										return;
									case "mapi":
										AddImageTabModule(path);
										return;
								}
							}
							MetroMessageBox.Show("Unsupported BLF Type", "The selected BLF file is not supported in assembly.");
							return;

						default:
							MetroMessageBox.Show("Unsupported file type", "The selected file is not supported in assembly.");
							return;
					}
				}

				MetroMessageBox.Show("Unable to find file", "The selected file could no longer be found");
			}
			catch (Exception ex)
			{
				MetroException.Show(ex);
			}
		}

		#endregion

		private void MenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
		{
			if (sender is MenuItem)
			{
				// make sure we have an xbdmdevicecollection
				if (App.AssemblyStorage.AssemblySettings.Xbdm is XbdmDeviceCollection)
				{
					XbdmDeviceCollection xbdmDeviceCollection = (XbdmDeviceCollection)App.AssemblyStorage.AssemblySettings.Xbdm;
					IList<XbdmDevice> devices = xbdmDeviceCollection.XbdmDevices;

					// get sender, loop through items
					MenuItem parentMenuItem = (MenuItem)sender;
					foreach (Control item in parentMenuItem.Items)
					{
						// if item is menu item, then add devices
						if (item is MenuItem)
						{
							MenuItem currentMenuItem = (MenuItem)item;

							// clear current items
							currentMenuItem.Items.Clear();

							// if we have 0 devices, tell them
							if (devices.Count < 1)
							{
								MenuItem noDevicesItem = new MenuItem();
								noDevicesItem.Header = "No Devices";
								noDevicesItem.IsEnabled = false;

								currentMenuItem.Items.Add(noDevicesItem);
							}
							else
							{
								// create all devices item
								MenuItem allDevicesItem = new MenuItem();
								allDevicesItem.Header = "All Devices";
								allDevicesItem.Tag = xbdmDeviceCollection;
								allDevicesItem.Click += xboxDevice_Click;

								currentMenuItem.Items.Add(allDevicesItem);
								currentMenuItem.Items.Add(new Separator());

								// add every device
								foreach (XbdmDevice device in devices)
								{
									MenuItem newItem = new MenuItem();
									newItem.Header = device.DeviceIdent;
									newItem.Tag = device;
									newItem.Click += xboxDevice_Click;
									currentMenuItem.Items.Add(newItem);
								}
							}
						}
					}
				}
			}
		}

		void xboxDevice_Click(object sender, RoutedEventArgs e)
		{
			if (sender is MenuItem)
			{
				MenuItem currentItem = (MenuItem)sender;
				MenuItem parentMenuItem = (MenuItem)currentItem.Parent;

				if (!(currentItem.Tag is IXbdm))
					return;
				string parentHeader = (string)parentMenuItem.Header;

				IXbdm xbdm = (IXbdm)currentItem.Tag;
				if (parentHeader == "Take Screenshot")
					takeScreenshot(xbdm);
				else if (parentHeader == "Freeze")
					xbdm.Freeze();
				else if (parentHeader == "Unfreeze")
					xbdm.Unfreeze();
				else if (parentHeader == "Title Reboot")
					xbdm.Reboot(RebootType.Title);
				else if (parentHeader == "Cold Reboot")
					xbdm.Reboot(RebootType.Cold);
			}
		}
	}
}