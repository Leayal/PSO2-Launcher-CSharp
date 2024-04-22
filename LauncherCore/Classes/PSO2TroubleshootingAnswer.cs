using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes
{
    public sealed class PSO2TroubleshootingAnswer : IReadOnlyList<PSO2TroubleshootingAnswer>
    {
        private readonly IReadOnlyList<PSO2TroubleshootingAnswer> _answers;

        public PSO2TroubleshootingAnswer(string name, string title, string tooltiptext, List<PSO2TroubleshootingAnswer>? answers)
        {
            this.Name = name;
            this.Title = title;
            this.TooltipText = tooltiptext;
            if (answers != null)
            {
                this._answers = answers.AsReadOnly();
            }
            else
            {
                this._answers = Array.Empty<PSO2TroubleshootingAnswer>();
            }
        }

        public string Name { get; }

        public string Title { get; }

        public string TooltipText { get; }

        public void Select()
        {
            this.Selected?.Invoke(this);
        }

        public event Action<PSO2TroubleshootingAnswer>? Selected;

        public PSO2TroubleshootingAnswer this[int index] => this._answers[index];

        public int Count => this._answers.Count;

        public IEnumerator<PSO2TroubleshootingAnswer> GetEnumerator()
        {
            return this._answers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_answers).GetEnumerator();
        }
    }
}
