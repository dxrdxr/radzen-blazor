﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenContextMenu component.
    /// </summary>
    /// <example>
    /// <code>
    /// @inject ContextMenuService ContextMenuService
    /// 
    /// &lt;RadzenButton Text="Show context menu" ContextMenu=@(args => ShowContextMenuWithItems(args)) /&gt;
    /// 
    /// @code {
    ///   void ShowContextMenuWithItems(MouseEventArgs args)
    ///   {
    ///     ContextMenuService.Open(args,
    ///         new List&lt;ContextMenuItem&gt; {
    ///             new ContextMenuItem() { Text = "Context menu item 1", Value = 1 },
    ///             new ContextMenuItem() { Text = "Context menu item 2", Value = 2 },
    ///             new ContextMenuItem() { Text = "Context menu item 3", Value = 3 },
    ///      }, OnMenuItemClick);
    ///   }
    ///   
    ///   void OnMenuItemClick(MenuItemEventArgs args)
    ///   {
    ///     Console.WriteLine($"Menu item with Value={args.Value} clicked");
    ///   }
    /// }
    /// </code>
    /// </example>
    public partial class RadzenContextMenu
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        /// <value>The unique identifier.</value>
        public string UniqueID { get; set; }

        /// <summary>
        /// Gets or sets the ContextMenuService.
        /// </summary>
        /// <value>The ContextMenuService.</value>
        [Inject] private ContextMenuService Service { get; set; }

        List<ContextMenu> menus = new List<ContextMenu>();

        /// <summary>
        /// Opens the menu.
        /// </summary>
        /// <param name="args">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        /// <param name="options">The options.</param>
        public async Task Open(MouseEventArgs args, ContextMenuOptions options)
        {
            menus.Clear();
            menus.Add(new ContextMenu() { Options = options, MouseEventArgs = args });

            await InvokeAsync(() => { StateHasChanged(); });
        }

        private bool IsJSRuntimeAvailable { get; set; }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            IsJSRuntimeAvailable = true;

            var menu = menus.LastOrDefault();
            if (menu != null)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.openContextMenu",
                    menu.MouseEventArgs.ClientX,
                    menu.MouseEventArgs.ClientY,
                    UniqueID);
            }
        }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        public async Task Close()
        {
            var lastTooltip = menus.LastOrDefault();
            if (lastTooltip != null)
            {
                menus.Remove(lastTooltip);
                await JSRuntime.InvokeVoidAsync("Radzen.closePopup", UniqueID);
            }

            await InvokeAsync(() => { StateHasChanged(); });
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (IsJSRuntimeAvailable)
            {
#if NET6_0_OR_GREATER
                var runtimeTask = disposeAsync();

                Exception ex = runtimeTask?.Exception;
                if (ex != null)
                {
                    if (ex is AggregateException)
                    {
                        ex = ex?.InnerException;
                    }
                    if (ex is JSDisconnectedException)
                    {
                        IsJSRuntimeAvailable = false;
                    }
                }
#else
                _ = disposeAsync();
#endif
            }

            Service.OnOpen -= OnOpen;
            Service.OnClose -= OnClose;
            Service.OnNavigate -= OnNavigate;

            async Task disposeAsync() =>
                await JSRuntime.InvokeVoidAsync("Radzen.destroyPopup", UniqueID);
        }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            UniqueID = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("/", "-").Replace("+", "-").Substring(0, 10);

            Service.OnOpen += OnOpen;
            Service.OnClose += OnClose;
            Service.OnNavigate += OnNavigate;
        }

        void OnOpen(MouseEventArgs args, ContextMenuOptions options)
        {
            Open(args, options).ConfigureAwait(false);
        }

        void OnClose()
        {
            Close().ConfigureAwait(false);
        }

        void OnNavigate()
        {
            JSRuntime.InvokeVoidAsync("Radzen.closePopup", UniqueID);
        }
    }
}
