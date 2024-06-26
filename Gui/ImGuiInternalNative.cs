using System;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;

namespace RWBaker.Gui;

public enum FocusRequestFlags
{
    ImGuiFocusRequestFlags_None                 = 0,
    ImGuiFocusRequestFlags_RestoreFocusedChild  = 1 << 0,   // Find last focused child (if any) and focus it instead.
    ImGuiFocusRequestFlags_UnlessBelowModal     = 1 << 1,   // Do not set focus if the window is below a modal.
};

public static class ImGuiInternalNative
{
    // WINDOW CALLS
    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr igGetCurrentWindowRead();

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr igGetCurrentWindow();

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr igFindWindowByID(uint id);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe IntPtr igFindWindowByName(byte* name);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern void igUpdateWindowParentAndRootLinks(IntPtr window, ImGuiWindowFlags flags, IntPtr parent_window);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern void igUpdateWindowSkipRefresh(IntPtr window);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern Vector2 igCalcWindowNextAutoFitSize(IntPtr window);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool igIsWindowChildOf(IntPtr window, IntPtr potential_parent, bool popup_hierarchy);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool igIsWindowWithinBeginStackOf(IntPtr window, IntPtr potential_parent);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool igIsWindowAbove(IntPtr potential_above, IntPtr potential_below);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool igIsWindowNavFocusable(IntPtr window);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetWindowPos(IntPtr window, Vector2 pos, ImGuiCond cond = 0);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetWindowSize(IntPtr window, Vector2 size, ImGuiCond cond = 0);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetWindowCollapsed(IntPtr window, bool collapsed, ImGuiCond cond = 0);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetWindowHitTestHole(IntPtr window, Vector2 pos, Vector2 size);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetWindowHiddenAndSkipItemsForCurrentFrame(IntPtr window);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetWindowParentWindowForFocusRoute(IntPtr window, IntPtr parent_window);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern void igFocusWindow(IntPtr window, FocusRequestFlags flags = 0);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void igFocusTopMostWindowUnderOne(IntPtr under_this_window, IntPtr ignore_window, ImGuiViewport* filter_viewport, FocusRequestFlags flags);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern void igBringWindowToFocusFront(IntPtr window);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern void igBringWindowToDisplayFront(IntPtr window);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern void igBringWindowToDisplayBack(IntPtr window);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern void igBringWindowToDisplayBehind(IntPtr window, IntPtr above_window);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern void igFindWindowDisplayIndex(IntPtr window);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr igFindBottomMostVisibleWindowWithinBeginStack(IntPtr window);

    // DOCK BUILDER
    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void DockBuilderDockWindow(byte* window_name, uint node_id);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr DockBuilderGetNode(uint node_id);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr DockBuilderGetCentralNode(uint node_id);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint DockBuilderAddNode(uint node_id = 0, ImGuiDockNodeFlags flags = 0);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DockBuilderRemoveNode(uint node_id); // Remove node and all its child, undock all windows

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DockBuilderRemoveNodeDockedWindows(uint node_id, bool clear_settings_refs = true);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DockBuilderRemoveNodeChildNodes(uint node_id); // Remove all split/hierarchy. All remaining docked windows will be re-docked to the remaining root node (node_id).

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DockBuilderSetNodePos(uint node_id, Vector2 pos);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DockBuilderSetNodeSize(uint node_id, Vector2 size);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe uint DockBuilderSplitNode(uint node_id, ImGuiDir split_dir, float size_ratio_for_node_at_dir, uint* out_id_at_dir, uint* out_id_at_opposite_dir); // Create 2 child nodes in this parent node.

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void DockBuilderCopyDockSpace(uint src_dockspace_id, uint dst_dockspace_id, ImVector<IntPtr>* in_window_remap_pairs);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void DockBuilderCopyNode(uint src_node_id, uint dst_node_id, ImVector<uint>* out_node_remap_pairs);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void DockBuilderCopyWindowSettings(byte* src_name, byte* dst_name);

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DockBuilderFinish(uint node_id);
}