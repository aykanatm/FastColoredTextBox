using System;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace FastColoredTextBoxNS
{
    public partial class FindForm : Form
    {
        string currentPattern;
        string previousPattern;
        bool patternFound = false;
        bool newPatternEntered = false;

        bool firstSearch = true;
        Place startPlace;
        FastColoredTextBox tb;

        public FindForm(FastColoredTextBox tb)
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

            FindNext(currentPattern);
        }

        public virtual void FindNext(string pattern)
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
                    FindNext(pattern);
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
            {
                btFindNext.PerformClick();
                e.Handled = true;
                return;
            }
            if (e.KeyChar == '\x1b')
            {
                Hide();
                e.Handled = true;
                return;
            }
        }

        private void FindForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
            this.tb.Focus();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                this.Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
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
