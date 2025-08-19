using System.Collections;
using System.Collections.Specialized;

namespace MauiFlow.Behaviors
{
    /// <summary>
    /// A behavior for <see cref="CollectionView"/> that automatically scrolls 
    /// to the last item when new items are added or when item sizes change.
    /// Handles dynamic <see cref="ItemsSource"/> changes and ensures UI is ready.
    /// </summary>
    public class ScrollToEndBehavior : Behavior<CollectionView>
    {
        CollectionView _collectionView;
        INotifyCollectionChanged _observableCollection;
        bool _isScrolling = false;
        DateTime _lastScrollTime = DateTime.MinValue;
        private const int SCROLL_DEBOUNCE_MS = 100; // Prevent excessive scrolling

        protected override void OnAttachedTo(CollectionView bindable)
        {
            base.OnAttachedTo(bindable);
            _collectionView = bindable;
            _collectionView.BindingContextChanged += OnBindingContextChanged;
            _collectionView.SizeChanged += OnCollectionViewSizeChanged;
            _collectionView.Loaded += OnCollectionViewLoaded;
            UpdateCollectionSubscription();
        }

        protected override void OnDetachingFrom(CollectionView bindable)
        {
            _collectionView.BindingContextChanged -= OnBindingContextChanged;
            _collectionView.SizeChanged -= OnCollectionViewSizeChanged;
            _collectionView.Loaded -= OnCollectionViewLoaded;
            UnsubscribeFromCollection();
            _collectionView = null;
            base.OnDetachingFrom(bindable);
        }

        private void OnBindingContextChanged(object sender, System.EventArgs e)
        {
            UpdateCollectionSubscription();
        }

        private void OnCollectionViewSizeChanged(object sender, System.EventArgs e)
        {
            // Handle cases where the CollectionView itself changes size
            // which might affect item layout
            ScrollToEndWithDebounce();
        }

        private void OnCollectionViewLoaded(object sender, System.EventArgs e)
        {
            // Ensure we scroll to end when the view is fully loaded
            ScrollToEndWithDebounce();
        }

        private void UpdateCollectionSubscription()
        {
            UnsubscribeFromCollection();
            if (_collectionView?.ItemsSource is INotifyCollectionChanged collection)
            {
                _observableCollection = collection;
                _observableCollection.CollectionChanged += OnCollectionChanged;
            }
        }

        private void UnsubscribeFromCollection()
        {
            if (_observableCollection != null)
            {
                _observableCollection.CollectionChanged -= OnCollectionChanged;
                _observableCollection = null;
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add ||
                e.Action == NotifyCollectionChangedAction.Reset ||
                e.Action == NotifyCollectionChangedAction.Replace) // Also handle replacements
            {
                ScrollToEndWithDebounce();
            }
        }

        void ScrollToEndWithDebounce()
        {
            if (_isScrolling || (DateTime.Now - _lastScrollTime).TotalMilliseconds < SCROLL_DEBOUNCE_MS)
                return;

            _isScrolling = true;
            _lastScrollTime = DateTime.Now;

            _collectionView.Dispatcher.Dispatch(async () =>
            {
                try
                {
                    // Multiple yields to ensure layout is completely updated
                    await Task.Yield();
                    await Task.Delay(10); // Small delay for layout completion

                    var items = _collectionView.ItemsSource as IEnumerable;
                    var lastItem = items?.Cast<object>().LastOrDefault();

                    if (lastItem != null)
                    {
                        // Try scrolling to the last item
                        _collectionView.ScrollTo(
                            item: lastItem,
                            position: ScrollToPosition.End,
                            animate: true);

                        // Additional fallback: scroll to end after a short delay
                        // This helps when item sizes are still changing
                        await Task.Delay(50);
                        _collectionView.ScrollTo(
                            item: lastItem,
                            position: ScrollToPosition.End,
                            animate: false); // Second scroll without animation
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ScrollTo failed: {ex.Message}");
                }
                finally
                {
                    _isScrolling = false;
                }
            });
        }
    }
}