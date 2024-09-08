/**
 * https://github.com/dotnet/maui/discussions/6695
 */

using Microsoft.Maui.Layouts;

namespace MapBoard.Views;
#region StretchDirection

/// <summary>
/// StretchDirection - Enum which describes when scaling should be used on the content of a Viewbox. This
/// enum restricts the scaling factors along various axes.
/// </summary>
public enum StretchDirection
{
    /// <summary>
    /// Only scales the content upwards when the content is smaller than the Viewbox.
    /// If the content is larger, no scaling downwards is done.
    /// </summary>
    UpOnly,

    /// <summary>
    /// Only scales the content downwards when the content is larger than the Viewbox.
    /// If the content is smaller, no scaling upwards is done.
    /// </summary>
    DownOnly,

    /// <summary>
    /// Always stretches to fit the Viewbox according to the stretch mode.
    /// </summary>
    Both
}

#endregion

public class ViewBox : Layout
{
    #region Public Fields

    public static readonly BindableProperty StretchProperty =
            BindableProperty.Create(nameof(Stretch), typeof(Stretch), typeof(ViewBox),
                                    Stretch.Uniform, propertyChanged: HandlePropertyChanged);

    private static void HandlePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (!(bindable is ViewBox viewBox))
            return;

        viewBox.InvalidateMeasure();
    }

    public Stretch Stretch
    {
        get => (Stretch)GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    public static readonly BindableProperty StretchDirectionProperty =
        BindableProperty.Create(nameof(StretchDirection), typeof(StretchDirection), typeof(ViewBox),
                                StretchDirection.Both, propertyChanged: HandlePropertyChanged);

    public StretchDirection StretchDirection
    {
        get => (StretchDirection)GetValue(StretchDirectionProperty);
        set => SetValue(StretchDirectionProperty, value);
    }

    #endregion

    public ViewBox() { }

    protected override void OnChildAdded(Element child)
    {
        if (!(child is View view))
            throw new ArgumentException(nameof(child));

        view.PropertyChanged += ViewPropertyChanged;
        base.OnChildAdded(child);
    }

    protected override void OnChildRemoved(Element child, int oldLogicalIndex)
    {
        if (child is View view)
            view.PropertyChanged -= ViewPropertyChanged;

        base.OnChildRemoved(child, oldLogicalIndex);
    }

    private void ViewPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (!(sender is View view))
            return;

        if (e.PropertyName == "VerticalOptions" || e.PropertyName == "HorizontalOptions")
            InvalidateMeasure();
    }

    protected override ILayoutManager CreateLayoutManager()
    {
        return new ViewBoxLayoutManager(this);
    }
}

public class ViewBoxLayoutManager : ILayoutManager
{
    private ViewBox _viewBox;

    private View? InternalChild => _viewBox.Children.FirstOrDefault() as View;

    public ViewBoxLayoutManager(ViewBox viewBox)
    {
        _viewBox = viewBox;
    }

    public Size Measure(double widthConstraint, double heightConstraint)
    {
        Size constraint = new(widthConstraint, heightConstraint);
        View? child = InternalChild;
        Size parentSize = new();

        try
        {
            if (child is not null)
            {
                // Initialize child constraint to infinity.  We need to get a "natural" size for the child
                // in absence of constraint.
                // Note that an author *can* impose a constraint on a child by using Height/Width, &c... properties 
                child.Measure(double.PositiveInfinity, double.PositiveInfinity);
                Size childSize = child.DesiredSize;

                Size scalefac = ComputeScaleFactor(constraint, childSize,
                                                   _viewBox.Stretch, _viewBox.StretchDirection);

                parentSize.Width = scalefac.Width * childSize.Width;
                parentSize.Height = scalefac.Height * childSize.Height;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.Print(ex.Message);
        }

        return parentSize;
    }

    public Size ArrangeChildren(Rect bounds)
    {
        Size arrangeSize = bounds.Size;
        View? child = InternalChild;
        double xOffset;
        double yOffset;

        try
        {
            if (child is not null)
            {
                Size childSize = child.DesiredSize;

                // Compute scaling factors from arrange size and the measured child content size
                Size scalefac = ComputeScaleFactor(arrangeSize, childSize,
                                                   _viewBox.Stretch, _viewBox.StretchDirection);

                //return the size oocupied by scaled child
                arrangeSize.Width = scalefac.Width * childSize.Width;
                arrangeSize.Height = scalefac.Height * childSize.Height;

                child.AnchorX = 0;
                child.AnchorY = 0;
                child.ScaleX = scalefac.Width;
                child.ScaleY = scalefac.Height;

                yOffset = child.VerticalOptions.Alignment switch
                {
                    LayoutAlignment.Start => 0,
                    LayoutAlignment.End => bounds.Height - arrangeSize.Height,
                    _ => (bounds.Height - arrangeSize.Height) / 2.0,
                };
                xOffset = child.HorizontalOptions.Alignment switch
                {
                    LayoutAlignment.Start => 0,
                    LayoutAlignment.End => bounds.Width - arrangeSize.Width,
                    _ => (bounds.Width - arrangeSize.Width) / 2.0,
                };

                // Arrange the child to the desired size 
                ((IView)child).Arrange(new Rect(new Point(xOffset, yOffset), child.DesiredSize));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.Print(ex.Message);
        }

        return arrangeSize;
    }

    /// <summary>
    /// This is a helper function that computes scale factors depending on a target size and a content size
    /// </summary>
    /// <param name="availableSize">Size into which the content is being fitted.</param>
    /// <param name="contentSize">Size of the content, measured natively (unconstrained).</param>
    /// <param name="stretch">Value of the Stretch property on the element.</param>
    /// <param name="stretchDirection">Value of the StretchDirection property on the element.</param>
    internal static Size ComputeScaleFactor(Size availableSize, Size contentSize,
                                            Stretch stretch, StretchDirection stretchDirection)
    {
        // Compute scaling factors to use for axes
        double scaleX = 1.0;
        double scaleY = 1.0;

        try
        {
            bool isConstrainedWidth = !double.IsPositiveInfinity(availableSize.Width);
            bool isConstrainedHeight = !double.IsPositiveInfinity(availableSize.Height);

            if ((stretch == Stretch.Uniform || stretch == Stretch.UniformToFill || stretch == Stretch.Fill)
                 && (isConstrainedWidth || isConstrainedHeight))
            {
                // Compute scaling factors for both axes
                scaleX = (IsZero(contentSize.Width)) ? 0.0 : availableSize.Width / contentSize.Width;
                scaleY = (IsZero(contentSize.Height)) ? 0.0 : availableSize.Height / contentSize.Height;

                if (!isConstrainedWidth) scaleX = scaleY;
                else if (!isConstrainedHeight) scaleY = scaleX;
                else
                {
                    // If not preserving aspect ratio, then just apply transform to fit
                    switch (stretch)
                    {
                        case Stretch.Uniform:       //Find minimum scale that we use for both axes
                            double minscale = scaleX < scaleY ? scaleX : scaleY;
                            scaleX = scaleY = minscale;
                            break;

                        case Stretch.UniformToFill: //Find maximum scale that we use for both axes
                            double maxscale = scaleX > scaleY ? scaleX : scaleY;
                            scaleX = scaleY = maxscale;
                            break;

                        case Stretch.Fill:          //We already computed the fill scale factors above, so just use them
                            break;
                    }
                }

                //Apply stretch direction by bounding scales.
                //In the uniform case, scaleX=scaleY, so this sort of clamping will maintain aspect ratio
                //In the uniform fill case, we have the same result too.
                //In the fill case, note that we change aspect ratio, but that is okay
                switch (stretchDirection)
                {
                    case StretchDirection.UpOnly:
                        if (scaleX < 1.0) scaleX = 1.0;
                        if (scaleY < 1.0) scaleY = 1.0;
                        break;

                    case StretchDirection.DownOnly:
                        if (scaleX > 1.0) scaleX = 1.0;
                        if (scaleY > 1.0) scaleY = 1.0;
                        break;

                    case StretchDirection.Both:
                        break;

                    default:
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.Print(ex.Message);
        }

        //Return this as a size now
        return new Size(scaleX, scaleY);
    }

    /// <summary>
    /// IsZero - Returns whether or not the double is "close" to 0.  Same as AreClose(double, 0),
    /// but this is faster.
    /// </summary>
    /// <returns>
    /// bool - the result of the AreClose comparision.
    /// </returns>
    /// <param name="value"> The double to compare to 0. </param>
    public static bool IsZero(double value) => Math.Abs(value) < 10.0 * double.Epsilon;
}