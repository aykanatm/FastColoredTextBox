using System;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace FastColoredTextBoxNS
{
    public partial class ReplaceForm : Form
    {
        string currentPattern;
        string previousPattern;
        bool patternFound = false;
        bool newPatternEntered = false;

        FastColoredTextBox tb;
        bool firstSearch = true;
        Place startPlace;

        public ReplaceForm(FastColoredTextBox tb)
        {
            InitializeComponent();
            this.tb = tb;
        }

        private void btClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btFindNext_Click(object sender, EventArgs e)
        {
            currentPattern = tbFind.Text;
            if (currentPattern != previousPattern)
            {
                newPatternEntered = true;
                previousPattern = currentPattern;
            }
            else
            {
                newPatternEntered = false;
            }

            Find(currentPattern);
        }

        public List<Range> FindAll(string pattern)
        {
            var opt = cbMatchCase.Checked ? RegexOptions.None : RegexOptions.IgnoreCase;
            if (!cbRegex.Checked)
                pattern = Regex.Escape(pattern);
            if (cbWholeWord.Checked)
                pattern = "\\b" + pattern + "\\b";
            //
            var range = tb.Selection.IsEmpty? tb.Range.Clone() : tb.Selection.Clone();
            //
            var list = new List<Range>();
            foreach (var r in range.GetRangesByLines(pattern, opt))
                list.Add(r);

            return list;
        }
        
        public virtual void Find(string pattern)
        {
            try
            {
                RegexOptions opt = cbMatchCase.Checked ? RegexOptions.None : RegexOptions.IgnoreCase;
                if (!cbRegex.Checked)
                    pattern = Regex.Escape(pattern);
                if (cbWholeWord.Checked)
                    pattern = "\\b" + pattern + "\\b";
                //
                Range range = tb.Selection.Clone();
                range.Normalize();
                //
                if (firstSearch)
                {
                    startPlace = range.Start;
                    firstSearch = false;
                }
                //
                range.Start = range.End;
                if (range.Start >= startPlace)
                    range.End = new Place(tb.GetLineLength(tb.LinesCount - 1), tb.LinesCount - 1);
                else
                    range.End = startPlace;
                //

                IEnumerable<Range> foundPatterns = range.GetRangesByLines(pattern, opt);
                if (CountRanges(foundPatterns) > 0)
                {
                    patternFound = true;

                    foreach (var r in foundPatterns)
                    {
                        tb.Selection = r;
                        tb.DoSelectionVisible();
                        tb.Invalidate();
                        return;
                    }
                }
                else
                {
                    patternFound = false;
                }

                //
                if (range.Start >= startPlace && startPlace > Place.Empty)
                {
                    tb.Selection.Start = new Place(0, 0);
                    Find(pattern);
                    return;
                }

                if (!patternFound && newPatternEntered)
                {
                    MessageBox.Show("The search phrase '" + pattern + "' is not found in the document.");
                }
                else
                {
                    startPlace = range.Start;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private int CountRanges(IEnumerable<Range> input)
        {
            int result = 0;
            using (IEnumerator<Range> enumerator = input.GetEnumerator())
            {
                while (enumerator.MoveNext())
                    result++;
            }
            return result;
        }

        private void tbFind_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
                btFindNext_Click(sender, null);
            if (e.KeyChar == '\x1b')
                Hide();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData) // David
        {
            if (keyData == Keys.Escape)
            {
                this.Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ReplaceForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
            this.tb.Focus();
        }

        private void btReplace_Click(object sender, EventArgs e)
        {
            try
            {
                if (tb.SelectionLength != 0)
                if (!tb.Selection.ReadOnly)
                    tb.InsertText(tbReplace.Text);
                btFindNext_Click(sender, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btReplaceAll_Click(object sender, EventArgs e)
        {
            try
            {
                tb.Selection.BeginUpdate();

                //search
                var ranges = FindAll(tbFind.Text);
                //check readonly
                var ro = false;
                foreach (var r in ranges)
                    if (r.ReadOnly)
                    {
                        ro = true;
                        break;
                    }
                //replace
                if (!ro)
                if (ranges.Count > 0)
                {
                    tb.TextSource.Manager.ExecuteCommand(new ReplaceTextCommand(tb.TextSource, ranges, tbReplace.Text));
                    tb.Selection.Start = new Place(0, 0);
                }
                //
                tb.Invalidate();
                MessageBox.Show(ranges.Count + " occurrence(s) replaced");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            tb.Selection.EndUpdate();
        }

        protected override void OnActivated(EventArgs e)
        {
            tbFind.Focus();
            ResetSerach();
        }

        void ResetSerach()
        {
            firstSearch = true;
        }

        private void cbMatchCase_CheckedChanged(object sender, EventArgs e)
        {
            ResetSerach();
        }
    }
}
