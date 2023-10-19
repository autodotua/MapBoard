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

    public int Length => 240;

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
        if (e.PropertyName is nameof(GeometryEditor.CanUndo) or nameof(GeometryEditor.CanRedo) or nameof(GeometryEditor.SelectedElement))
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
        stkSelection.IsVisible = MainMapView.Current.CurrentTask == BoardTask.Select;

        stkEdition.IsVisible = MainMapView.Current.CurrentTask == BoardTask.Draw;

        btnUndo.IsEnabled = MainMapView.Current.GeometryEditor.CanUndo;
        btnRedo.IsEnabled = MainMapView.Current.GeometryEditor.CanRedo;

        btnDeleteVertex.IsEnabled = MainMapView.Current.GeometryEditor.SelectedElement is GeometryEditorVertex;
    }

    private void DeleteVertexButton_Click(object sender, EventArgs e)
    {
        MainMapView.Current.GeometryEditor.DeleteSelectedElement();
    }

    private async void AttributeTableButton_Click(object sender, EventArgs e)
    {
        await MainPage.Current.DisplayAlert("属性表", "暂未实现此功能", "确定");
    }
}