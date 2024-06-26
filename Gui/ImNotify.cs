using System;
using System.Numerics;
using ImGuiNET;

namespace RWBaker.Gui;

public enum ImNotifyType
{
    None,
    Success,
    Warning,
    Error,
    Info
}

public enum ImNotifyPhase
{
    FadeIn,
    Wait,
    FadeOut,
    Expired
}

public enum ImNotifyPos
{
    TopLeft,
    TopCenter,
    TopRight,
    BottomLeft,
    BottomCenter,
    BottomRight,
    Center
}

public class ImNotify
{
    private const float PADDING_X = 20f;		  // Bottom-left X padding
    private const float PADDING_Y = 20f;		  // Bottom-left Y padding
    private const float PADDING_MESSAGE_Y = 10f;  // Padding Y between each message
    private const int FADE_IN_OUT_TIME = 150;    // Fade in and out duration
    private const float OPACITY = 0.8f;           // 0-1 Toast opacity
    private const bool USE_SEPARATOR = false;     // If true, a separator will be rendered between the title and the content
    private const bool USE_DISMISS_BUTTON = true; // If true, a dismiss button will be rendered in the top right corner of the toast

    private const ImGuiWindowFlags DEFAULT_NOTIFY_FLAGS = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoFocusOnAppearing;

    public string Title;
    public string Content;

    public Vector4 Color;

    public ImNotifyType Type = ImNotifyType.None;

    public string ButtonLabel;
    public Action ButtonAction = () => {};

    private readonly DateTime creationTime;
    private readonly int dismissInterval;

    private double _elapsed => (DateTime.Now - creationTime).TotalMilliseconds;

    private readonly string _icon;

    private ImGuiWindowFlags _windowFlags = DEFAULT_NOTIFY_FLAGS;
    private string _id = "";

    public ImNotify(ImNotifyType type, string content, string title = "", int dismissTime = 3000, string buttonLabel = "", Action? buttonAction = null)
    {
        string defaultTitle;

        switch (type)
        {
            case ImNotifyType.None:
                defaultTitle = string.Empty;
                Color = new Vector4(255, 255, 255, 255);
                _icon = string.Empty;
                break;

            case ImNotifyType.Success:
                defaultTitle = "Success";
                Color = new Vector4(0, 255, 0, 255);
                _icon = Codicons.Pass;
                break;

            case ImNotifyType.Warning:
                defaultTitle = "Warning";
                Color = new Vector4(255, 255, 0, 255);
                _icon = Codicons.Alert;
                break;

            case ImNotifyType.Error:
                defaultTitle = "Error";
                Color = new Vector4(255, 0, 0, 255);
                _icon = Codicons.Error;
                break;

            case ImNotifyType.Info:
                defaultTitle = "Info";
                Color = new Vector4(0, 255, 255, 255);
                _icon = Codicons.Info;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        Title = string.IsNullOrEmpty(title) ? defaultTitle : title;

        Content = content;

        creationTime = DateTime.Now;
        dismissInterval = dismissTime;

        ButtonLabel = buttonLabel;
        if (buttonAction != null) ButtonAction = buttonAction;
    }

    public void ManagerRegister(int id) => _id = $"##NOTIFY{id}";

    private ImNotifyPhase GetPhase()
    {
        double elapsed = _elapsed;

        if (elapsed > FADE_IN_OUT_TIME + dismissInterval + FADE_IN_OUT_TIME)
        {
            return ImNotifyPhase.Expired;
        }

        if (elapsed > FADE_IN_OUT_TIME + dismissInterval)
        {
            return ImNotifyPhase.FadeOut;
        }

        if (elapsed > FADE_IN_OUT_TIME)
        {
            return ImNotifyPhase.Wait;
        }

        return ImNotifyPhase.FadeIn;
    }

    private float GetFadePercent()
    {
        ImNotifyPhase phase = GetPhase();
        double elapsed = _elapsed;

        return phase switch
        {
            ImNotifyPhase.FadeIn => ((float)elapsed / FADE_IN_OUT_TIME) * OPACITY,
            ImNotifyPhase.FadeOut => (1f - ((float)elapsed - FADE_IN_OUT_TIME - dismissInterval) / FADE_IN_OUT_TIME) * OPACITY,
            _ => OPACITY
        };
    }

    public void Render(Vector2 viewportSize, ref float height)
    {
	    bool buttonLabelEmpty = ButtonLabel.Length == 0;
	    bool contentEmpty = Content.Length == 0;
	    bool iconEmpty = _icon.Length == 0;

		// Remove toast if expired
		if (GetPhase() == ImNotifyPhase.Expired)
		{
			GuiManager.RemoveNotification(this);
			return;
		}

		// Get icon, title and other data
	    float opacity = GetFadePercent(); // Get opacity based of the current phase

		// Window rendering
		Vector4 textColor = Color;
		textColor.W = opacity;

		//PushStyleColor(ImGuiCol_Text, textColor);
		ImGui.SetNextWindowBgAlpha(opacity);

		// Set notification window position to bottom right corner of the main window, considering the main window size and location in relation to the display
		Vector2 viewportPos = ImGui.GetMainViewport().Pos;
		ImGui.SetNextWindowPos(new Vector2(viewportPos.X + viewportSize.X - PADDING_X, viewportPos.Y + viewportSize.Y - PADDING_Y - height), ImGuiCond.Always, Vector2.One);

		// Set notification window flags
		if (!USE_DISMISS_BUTTON && buttonLabelEmpty)
		{
			_windowFlags = (DEFAULT_NOTIFY_FLAGS | ImGuiWindowFlags.NoInputs);
		}

		ImGui.Begin(_id, _windowFlags);
		ImGuiInternalNative.igBringWindowToDisplayFront(ImGuiInternalNative.igGetCurrentWindow());

		// Here we render the toast content
		{
			ImGui.PushTextWrapPos(viewportSize.X / 3f); // We want to support multi-line text, this will wrap the text after 1/3 of the screen width

			// If an icon is set
			if (!iconEmpty)
			{
				//Text(icon); // Render icon text
				ImGui.TextColored(textColor, _icon);
			}

			// If a title and an icon is set, we want to render on same line
			if (!iconEmpty) ImGui.SameLine();

			ImGui.Text(Title); // Render title text

			// If a dismiss button is enabled
			if (USE_DISMISS_BUTTON)
			{
				// If a title or content is set, we want to render the button on the same line
				if (!contentEmpty)
				{
					ImGui.SameLine();
				}

				// Render the dismiss button in the top right corner
				// NEEDS TO BE REWORKED
				float scale = 0.8f;

				if (ImGui.CalcTextSize(Content).X > ImGui.GetWindowContentRegionMax().X)
				{
					scale = 0.95f;
				}

				ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetWindowSize().X - ImGui.GetCursorPosX()) * scale);

				// If the button is pressed, we want to remove the notification
				if (ImGui.Button(Codicons.Close))
				{
					GuiManager.RemoveNotification(this);
				}
			}

			// In case ANYTHING was rendered in the top, we want to add a small padding so the text (or icon) looks centered vertically
			if (!contentEmpty)
			{
				ImGui.SetCursorPos(ImGui.GetCursorPos() + Vector2.UnitY * 5); // Must be a better way to do this!!!!
			}

			// If a content is set
			if (!contentEmpty)
			{
				if (USE_SEPARATOR) ImGui.Separator();

				ImGui.Text(Content); // Render content text
			}

			// If a button is set
			if (!buttonLabelEmpty)
			{
				// If the button is pressed, we want to execute the lambda function
				if (ImGui.Button(ButtonLabel)) ButtonAction();
			}

			ImGui.PopTextWrapPos();
		}

		// Save height for next toasts
		height += ImGui.GetWindowHeight() + PADDING_MESSAGE_Y;

		ImGui.End();
    }
}