using System.Collections.ObjectModel;
using HelixToolkit.Wpf.SharpDX;
using System.Windows.Controls;
using L2Viewer.SceneDomain.Models;
using L2Viewer.SceneDomain.Services;

namespace L2Viewer.Wpf;

internal sealed class SkeletalAnimationPreviewController
{
    private readonly MainWindow _owner;
    private readonly ScenePreviewRenderer _renderer;
    private readonly ObservableCollection<MainWindow.SkeletalAnimationListItem> _items;

    private ISkeletalAnimationPreviewSession? _currentSession;
    private MainWindow.SkeletalAnimationListItem? _currentItem;
    private DateTime _currentStartedUtc = DateTime.MinValue;
    private bool _isUpdatingFrameControls;
    private int _currentFrameIndex;
    private int _playbackStartFrameIndex;

    public SkeletalAnimationPreviewController(
        MainWindow owner,
        ScenePreviewRenderer renderer,
        ObservableCollection<MainWindow.SkeletalAnimationListItem> items)
    {
        _owner = owner;
        _renderer = renderer;
        _items = items;
    }

    private Border AnimationPanelBorder => _owner.AnimationPanelBorderControl;
    private TextBlock AnimationSummaryTextBlock => _owner.AnimationSummaryTextBlockControl;
    private ListBox AnimationListBox => _owner.AnimationListBoxControl;
    private CheckBox AnimateSkeletalPreviewCheckBox => _owner.AnimateSkeletalPreviewCheckBoxControl;
    private CheckBox LoopAnimationCheckBox => _owner.LoopAnimationCheckBoxControl;
    private Slider AnimationFrameSlider => _owner.AnimationFrameSliderControl;
    private TextBlock AnimationFrameTextBlock => _owner.AnimationFrameTextBlockControl;
    private Button PrevAnimationFrameButton => _owner.PrevAnimationFrameButtonControl;
    private Button NextAnimationFrameButton => _owner.NextAnimationFrameButtonControl;
    private LineGeometryModel3D BoneOverlayModel => _owner.BoneOverlayModel;
    private PointGeometryModel3D VertexDebugModel => _owner.VertexDebugModel;
    private TextBlock PreviewSubtitleTextBlock => _owner.PreviewSubtitleTextBlockControl;
    private TextBlock StatusTextBlock => _owner.StatusTextBlock;

    public void Reset()
    {
        _currentSession = null;
        _currentItem = null;
        _currentStartedUtc = DateTime.MinValue;
        _currentFrameIndex = 0;
        _playbackStartFrameIndex = 0;
        BoneOverlayModel.Geometry = null;
        VertexDebugModel.Geometry = null;
        AnimationPanelBorder.Visibility = Visibility.Collapsed;
        AnimationSummaryTextBlock.Text = "No animation data";
        AnimationListBox.SelectedItem = null;
        LoopAnimationCheckBox.IsEnabled = true;
        UpdateFrameControls(null, 0);
        _items.Clear();
    }

    public void Bind(ISkeletalAnimationPreviewSession session)
    {
        _currentSession = session;
        _currentItem = null;
        _currentStartedUtc = DateTime.MinValue;
        _currentFrameIndex = 0;
        _playbackStartFrameIndex = 0;

        _items.Clear();
        foreach (var animation in session.Animations.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            _items.Add(new MainWindow.SkeletalAnimationListItem(
                $"{animation.Name}  |  {animation.DurationSeconds:0.###}s  |  {animation.NumFrames}f  |  tracks {animation.TrackCount}",
                animation.Name,
                animation.DurationSeconds,
                animation.NumFrames,
                animation.TrackCount));
        }

        AnimationSummaryTextBlock.Text = _items.Count == 0
            ? "No animation sequences found"
            : $"Found {_items.Count} sequences";
        AnimationPanelBorder.Visibility = _items.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        LoopAnimationCheckBox.IsEnabled = AnimateSkeletalPreviewCheckBox.IsChecked != false;
        if (_items.Count > 0)
        {
            AnimationListBox.SelectedIndex = 0;
        }
        else
        {
            UpdateFrameControls(null, 0);
        }
    }

    public void UpdateAnimationFrame()
    {
        if (_currentSession is null || _currentItem is null || AnimateSkeletalPreviewCheckBox.IsChecked == false)
        {
            return;
        }

        var frameIndex = ComputePlaybackFrameIndex(_currentItem, out var finished);
        var mesh = _currentSession.BuildAnimatedMeshFrame(_currentItem.SequenceName, frameIndex);
        var overlay = _currentSession.BuildAnimatedOverlayFrame(_currentItem.SequenceName, frameIndex);
        if (mesh is null || overlay is null)
        {
            return;
        }

        var scene = _renderer.PrepareMeshScene(mesh);
        _owner.MeshModel.Geometry = scene.Geometry;
        _owner.BoundsModel.Geometry = scene.Bounds;
        BoneOverlayModel.Geometry = _renderer.BuildBoneOverlayGeometry(overlay);
        VertexDebugModel.Geometry = null;
        UpdateFrameControls(_currentItem, frameIndex);
        if (finished && LoopAnimationCheckBox.IsChecked == false)
        {
            _currentStartedUtc = DateTime.MinValue;
            _playbackStartFrameIndex = Math.Max(0, _currentItem.NumFrames - 1);
        }
    }

    public void AnimationSelectionChanged()
    {
        if (_currentSession is null)
        {
            return;
        }

        if (AnimationListBox.SelectedItem is not MainWindow.SkeletalAnimationListItem item)
        {
            _currentItem = null;
            return;
        }

        _currentItem = item;
        _currentStartedUtc = DateTime.UtcNow;
        _playbackStartFrameIndex = 0;
        if (AnimateSkeletalPreviewCheckBox.IsChecked == false)
        {
            ShowSelectedAnimationFrame(item, 0, "Paused on");
            return;
        }

        ShowSelectedAnimationAtStart(item);
    }

    public void AnimateToggleChanged()
    {
        LoopAnimationCheckBox.IsEnabled = AnimateSkeletalPreviewCheckBox.IsChecked != false;
        if (_currentSession is null)
        {
            return;
        }

        if (AnimateSkeletalPreviewCheckBox.IsChecked == false)
        {
            if (_currentItem is not null)
            {
                ShowSelectedAnimationFrame(_currentItem, _currentFrameIndex, "Paused on");
            }
            else
            {
                ShowCurrentBindPose();
            }
            return;
        }

        if (_currentItem is not null)
        {
            _currentStartedUtc = DateTime.UtcNow;
            _playbackStartFrameIndex = _currentFrameIndex;
            ShowSelectedAnimationFrame(_currentItem, _currentFrameIndex, "Playing");
            return;
        }

        ShowCurrentBindPose();
    }

    public void FrameSliderChanged()
    {
        if (_isUpdatingFrameControls || _currentSession is null || _currentItem is null)
        {
            return;
        }

        var frameIndex = (int)Math.Round(AnimationFrameSlider.Value);
        _currentStartedUtc = DateTime.UtcNow;
        _playbackStartFrameIndex = frameIndex;
        ShowSelectedAnimationFrame(_currentItem, frameIndex, AnimateSkeletalPreviewCheckBox.IsChecked == true ? "Showing" : "Paused on");
    }

    public void StepFrame(int delta)
    {
        if (_currentSession is null || _currentItem is null)
        {
            return;
        }

        var frameCount = Math.Max(1, _currentItem.NumFrames);
        var frameIndex = _currentFrameIndex + delta;
        if (LoopAnimationCheckBox.IsChecked == true)
        {
            frameIndex = ((frameIndex % frameCount) + frameCount) % frameCount;
        }
        else
        {
            frameIndex = Math.Clamp(frameIndex, 0, frameCount - 1);
        }

        _currentStartedUtc = DateTime.UtcNow;
        _playbackStartFrameIndex = frameIndex;
        ShowSelectedAnimationFrame(_currentItem, frameIndex, AnimateSkeletalPreviewCheckBox.IsChecked == true ? "Showing" : "Paused on");
    }

    private void ShowSelectedAnimationAtStart(MainWindow.SkeletalAnimationListItem item)
    {
        ShowSelectedAnimationFrame(item, 0, "Playing");
    }

    private void ShowCurrentBindPose()
    {
        if (_currentSession is null)
        {
            return;
        }

        var mesh = _currentSession.BuildBindPoseMesh();
        var overlay = _currentSession.BuildBindPoseOverlay();
        _renderer.ApplyPreparedMesh(mesh, _renderer.PrepareMeshScene(mesh), fitCamera: false);
        _owner.MeshModel.Material = _renderer.CreateSkeletalDiagnosticMaterial();
        _owner.MeshModel.CullMode = global::SharpDX.Direct3D11.CullMode.None;
        BoneOverlayModel.Geometry = _renderer.BuildBoneOverlayGeometry(overlay);
        VertexDebugModel.Geometry = null;
        _renderer.ShowMeshPreview();
        UpdateFrameControls(_currentItem, _currentFrameIndex);

        var selectedSuffix = _currentItem is null ? "no animation selected" : $"selected: {_currentItem.SequenceName}";
        PreviewSubtitleTextBlock.Text = $"Bind pose | {selectedSuffix} | bone skinning";
        StatusTextBlock.Text = "Showing skeletal bind pose using bone skinning.";
    }

    private void ShowSelectedAnimationFrame(MainWindow.SkeletalAnimationListItem item, int frameIndex, string statusPrefix)
    {
        if (_currentSession is null)
        {
            return;
        }

        var clampedFrame = Math.Clamp(frameIndex, 0, Math.Max(0, item.NumFrames - 1));
        var mesh = _currentSession.BuildAnimatedMeshFrame(item.SequenceName, clampedFrame);
        var overlay = _currentSession.BuildAnimatedOverlayFrame(item.SequenceName, clampedFrame);
        if (mesh is null || overlay is null)
        {
            return;
        }

        _currentFrameIndex = clampedFrame;
        _renderer.ApplyPreparedMesh(mesh, _renderer.PrepareMeshScene(mesh), fitCamera: false);
        _owner.MeshModel.Material = _renderer.CreateSkeletalDiagnosticMaterial();
        _owner.MeshModel.CullMode = global::SharpDX.Direct3D11.CullMode.None;
        BoneOverlayModel.Geometry = _renderer.BuildBoneOverlayGeometry(overlay);
        VertexDebugModel.Geometry = null;
        _renderer.ShowMeshPreview();
        UpdateFrameControls(item, clampedFrame);
        PreviewSubtitleTextBlock.Text = $"Animation: {item.SequenceName} | frame {clampedFrame + 1}/{Math.Max(1, item.NumFrames)} | {item.DurationSeconds:0.###} s | bone skinning";
        StatusTextBlock.Text = $"{statusPrefix} skeletal animation {item.SequenceName} frame {clampedFrame + 1}/{Math.Max(1, item.NumFrames)} using bone skinning.";
    }

    private int ComputePlaybackFrameIndex(MainWindow.SkeletalAnimationListItem item, out bool finished)
    {
        var frameCount = Math.Max(1, item.NumFrames);
        var duration = item.DurationSeconds > 0.0001f ? item.DurationSeconds : frameCount / 30.0;
        var frameDuration = Math.Max(1.0 / 120.0, duration / frameCount);
        var elapsed = Math.Max(0d, (DateTime.UtcNow - _currentStartedUtc).TotalSeconds);

        if (LoopAnimationCheckBox.IsChecked == true)
        {
            finished = false;
            return (_playbackStartFrameIndex + (int)Math.Floor(elapsed / frameDuration)) % frameCount;
        }

        var rawIndex = _playbackStartFrameIndex + (int)Math.Floor(elapsed / frameDuration);
        finished = rawIndex >= frameCount - 1;
        return Math.Clamp(rawIndex, 0, frameCount - 1);
    }

    private void UpdateFrameControls(MainWindow.SkeletalAnimationListItem? item, int frameIndex)
    {
        var maxFrame = item is null ? 0 : Math.Max(0, item.NumFrames - 1);
        var safeFrame = item is null ? 0 : Math.Clamp(frameIndex, 0, maxFrame);
        _currentFrameIndex = safeFrame;

        _isUpdatingFrameControls = true;
        AnimationFrameSlider.Minimum = 0;
        AnimationFrameSlider.Maximum = maxFrame;
        AnimationFrameSlider.IsEnabled = item is not null;
        AnimationFrameSlider.Value = safeFrame;
        AnimationFrameTextBlock.Text = item is null
            ? "Frame 0 / 0"
            : $"Frame {safeFrame + 1} / {Math.Max(1, item.NumFrames)}";
        PrevAnimationFrameButton.IsEnabled = item is not null;
        NextAnimationFrameButton.IsEnabled = item is not null;
        _isUpdatingFrameControls = false;
    }
}
