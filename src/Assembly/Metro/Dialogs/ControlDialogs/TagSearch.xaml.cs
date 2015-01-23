using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
    /// <summary>
    /// Interaction logic for TagSearch.xaml
    /// </summary>
    public partial class TagSearch : Window
    {
        private List<string> _tags;

        public TagSearch(List<string> tags)
        {
            InitializeComponent();

            // add all the tags to the matches list
            _tags = tags;
            foreach (string tag in tags)
                this.lstTags.Items.Add(tag);
        }

        public int SelectedTagIndex
        {
            get;
            private set;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            CloseWithSave();
        }

        private void lstTags_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.lstTags.SelectedItem != null)
                CloseWithSave();
        }

        private void CloseWithSave()
        {
            // find the index of the selected tag in the original list
            for (int i = 0; i < _tags.Count; i++)
                if (_tags[i] == (string)this.lstTags.SelectedValue)
                    this.SelectedTagIndex = i;

            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.SelectedTagIndex = -1;
            this.Close();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            // remove all the items from the matches box
            this.lstTags.Items.Clear();

            // add back the ones that match the regex
            foreach (string item in this._tags)
                if (Regex.IsMatch(item, this.txtSearch.Text))
                    this.lstTags.Items.Add(item);
        }
    }
}
