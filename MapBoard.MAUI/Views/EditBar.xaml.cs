using Esri.ArcGISRuntime.UI.Editing;
using MapBoard.Mapping;
using MapBoard.ViewModels;

namespace MapBoard.Views;

public partial class EditBar : ContentView, ISidePanel
{
    public EditBar()
    {
        InitializeComponent();
    }

    public SwipeDirection Direction => SwipeDirection.Up;

    public int Length => 120;

    public bool Standalone => true;

    public void OnPanelClosed()
    {
    }

    public void OnPanelOpening()
    {
        UpdateButtonsVisible();
    }

    private void CancelEdit_Click(object sender, EventArgs e)
    {
        MainMapView.Current.Editor.Cancel();
    }

    private void CancelSelectionButton_Click(object sender, EventArgs e)
    {
        MainMapView.Current.ClearSelection();
    }

    private void ContentView_Loaded(object sender, EventArgs e)
    {
        MainMapView.Current.GeometryEditor.PropertyChanged += GeometryEditor_PropertyChanged;
    }

    private void DeleteButton_Click(object sender, EventArgs e)
    {
        MainMapView.Current.DeleteSelectedFeatureAsync();
    }

    private void EditButton_Click(object sender, EventArgs e)
    {
        MainMapView.Current.Editor.StartEditSelection();
    }

    private void GeometryEditor_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(GeometryEditor.CanUndo) or nameof(GeometryEditor.CanRedo))
        {
            UpdateButtonsVisible();
        }
    }

    private void RedoButton_Click(object sender, EventArgs e)
    {
        MainMapView.Current.GeometryEditor.Redo();
    }

    private async void SaveEdit_Click(object sender, EventArgs e)
    {
        await MainMapView.Current.Editor.SaveAsync();
    }

    private void UndoButton_Click(object sender, EventArgs e)
    {
        MainMapView.Current.GeometryEditor.Undo();
    }

    private void UpdateButtonsVisible()
    {
        btnEdit.IsVisible = MainMapView.Current.CurrentTask == BoardTask.Select;
        btnClearSelection.IsVisible = MainMapView.Current.CurrentTask == BoardTask.Select;
        btnDelete.IsVisible = MainMapView.Current.CurrentTask == BoardTask.Select;

        btnSaveEdit.IsVisible = MainMapView.Current.CurrentTask == BoardTask.Draw;
        btnCancelEdit.IsVisible = MainMapView.Current.CurrentTask == BoardTask.Draw;
        btnUndo.IsVisible = MainMapView.Current.CurrentTask == BoardTask.Draw;
        btnRedo.IsVisible = MainMapView.Current.CurrentTask == BoardTask.Draw;

        btnUndo.IsEnabled = MainMapView.Current.GeometryEditor.CanUndo;
        btnRedo.IsEnabled = MainMapView.Current.GeometryEditor.CanRedo;
    }
}