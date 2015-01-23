using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembly.Metro.Dialogs
{
    public static class TagSearchDialog
    {
        public static int Show(List<string> tags)
        {
            var dialog = new Assembly.Metro.Dialogs.ControlDialogs.TagSearch(tags);
            dialog.ShowDialog();

            return dialog.SelectedTagIndex;
        }
    }
}
