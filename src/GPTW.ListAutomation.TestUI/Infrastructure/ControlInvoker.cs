using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GPTW.ListAutomation.TestUI.Infrastructure
{
    public static class ControlInvoker
    {
        public static void Invoke(Control ctl, MethodInvoker method)
        {
            if (!ctl.IsHandleCreated)
                return;

            if (ctl.IsDisposed)
                return;

            if (ctl.InvokeRequired)
            {
                ctl.Invoke(method);
            }
            else
            {
                method();
            }
        }
    }
}
