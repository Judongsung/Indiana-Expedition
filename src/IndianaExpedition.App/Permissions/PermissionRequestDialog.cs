using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using IndianaExpedition.Permissions;
using IndianaExpedition.Resources;
using IndianaExpedition.Styling;

namespace IndianaExpedition.Permissions
{
    internal sealed class PermissionRequestDialog : LunaForm
    {
        private static readonly IReadOnlyDictionary<PermissionPromptDecision, PermissionPromptResponse>
            ResponseByDecision =
                new ReadOnlyDictionary<PermissionPromptDecision, PermissionPromptResponse>(
                    new Dictionary<PermissionPromptDecision, PermissionPromptResponse>
                    {
                        [PermissionPromptDecision.AllowOnce] = new PermissionPromptResponse(
                            CoreWebView2PermissionState.Allow,
                            saveInProfile: false),
                        [PermissionPromptDecision.AlwaysAllow] = new PermissionPromptResponse(
                            CoreWebView2PermissionState.Allow,
                            saveInProfile: true),
                        [PermissionPromptDecision.BlockOnce] = new PermissionPromptResponse(
                            CoreWebView2PermissionState.Deny,
                            saveInProfile: false),
                        [PermissionPromptDecision.AlwaysBlock] = new PermissionPromptResponse(
                            CoreWebView2PermissionState.Deny,
                            saveInProfile: true)
                    });

        internal PermissionRequestDialog(
            string origin,
            CoreWebView2PermissionKind kind,
            bool preventActivationOnShow = false)
        {
            PreventActivationOnShow = preventActivationOnShow;
            Text = Strings.PermissionRequestTitle;
            SetContentClientSize(520, 246);
            LunaResizable = false;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = preventActivationOnShow;
            StartPosition = preventActivationOnShow
                ? FormStartPosition.CenterScreen
                : FormStartPosition.CenterParent;

            var heading = new Label
            {
                Text = Strings.PermissionRequestHeading,
                Font = new Font(Font, FontStyle.Bold),
                Location = new Point(22, 20),
                Size = new Size(476, 24)
            };
            var site = new Label
            {
                Text = string.Format(Strings.PermissionSiteFormat, NormalizeOrigin(origin)),
                Location = new Point(22, 54),
                Size = new Size(476, 38),
                AutoEllipsis = true
            };
            var permission = new Label
            {
                Text = string.Format(
                    Strings.PermissionKindFormat,
                    PermissionKindDisplay.GetText(kind)),
                Location = new Point(22, 98),
                Size = new Size(476, 38)
            };
            var explanation = new Label
            {
                Text = Strings.PermissionChoiceExplanation,
                Location = new Point(22, 142),
                Size = new Size(476, 36)
            };

            ContentPanel.Controls.AddRange(new Control[]
            {
                heading,
                site,
                permission,
                explanation,
                CreateDecisionButton(Strings.PermissionAllowOnce, 22, PermissionPromptDecision.AllowOnce),
                CreateDecisionButton(Strings.PermissionAlwaysAllow, 142, PermissionPromptDecision.AlwaysAllow),
                CreateDecisionButton(Strings.PermissionBlockOnce, 282, PermissionPromptDecision.BlockOnce),
                CreateDecisionButton(Strings.PermissionAlwaysBlock, 402, PermissionPromptDecision.AlwaysBlock)
            });

            Response = ResponseByDecision[PermissionPromptDecision.BlockOnce];
        }

        internal PermissionPromptResponse Response { get; private set; }

        private XpButton CreateDecisionButton(
            string text,
            int left,
            PermissionPromptDecision decision)
        {
            var button = new XpButton
            {
                Text = text,
                Location = new Point(left, 194),
                Size = decision == PermissionPromptDecision.AlwaysAllow ||
                       decision == PermissionPromptDecision.AlwaysBlock
                    ? new Size(110, 27)
                    : new Size(100, 27)
            };
            button.Click += (sender, args) =>
            {
                Response = ResponseByDecision[decision];
                DialogResult = DialogResult.OK;
                Close();
            };
            return button;
        }

        private static string NormalizeOrigin(string value)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out var uri)
                ? uri.GetLeftPart(UriPartial.Authority)
                : value;
        }
    }
}
