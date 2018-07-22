using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using DwarfCorp.Trade;

namespace DwarfCorp.Gui.Widgets
{
    public class ResourceColumns : Columns
    {
        public ITradeEntity TradeEntity;
        public ITradeEntity ValueSourceEntity;
        public List<ResourceAmount> SourceResources { get; private set; }
        public List<ResourceAmount> SelectedResources { get; private set; }
        public String LeftHeader;
        public String RightHeader;
        private MoneyEditor MoneyField;
        public String MoneyLabel;

        public DwarfBux TradeMoney
        {
            get { return (decimal)MoneyField.CurrentValue; }
            set { MoneyField.CurrentValue = (int) value; }
        }
        public bool Valid { get { return MoneyField.Valid; } }
        
        public int TotalSelectedItems
        {
            get
            {
                return SelectedResources.Sum(r => r.NumResources);
            }
        }

        public Action<Widget> OnTotalSelectedChanged;

        public ResourceColumns()
        {
            SourceResources = new List<ResourceAmount>();
            SelectedResources = new List<ResourceAmount>();
        }

        public void Reconstruct(IEnumerable<ResourceAmount> sourceResource, 
                                IEnumerable<ResourceAmount> selectedResources,
                                int tradeMoney)
        {
            Clear();
            SourceResources = Clone(sourceResource.ToList());
            SelectedResources = Clone(selectedResources.ToList());

            var leftPanel = AddChild(new Widget());


            leftPanel.AddChild(new Gui.Widget
            {
                Text = LeftHeader,
                Font = "font16",
                TextColor = new Vector4(0, 0, 0, 1),
                AutoLayout = AutoLayout.DockTop
            });

            leftPanel.AddChild(new Gui.Widget
            {
                MinimumSize = new Point(0, 32),
                AutoLayout = AutoLayout.DockBottom,
                Font = "font10",
                TextColor = new Vector4(0, 0, 0, 1),
                Text = String.Format(MoneyLabel + ": {0}", TradeEntity.Money.ToString()),
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center
            });

            var leftList = leftPanel.AddChild(new Gui.Widgets.WidgetListView
            {
                ItemHeight = 32,
                AutoLayout = AutoLayout.DockFill,
                SelectedItemBackgroundColor = new Vector4(0, 0, 0, 0),
                ItemBackgroundColor2 = new Vector4(0, 0, 0, 0.1f),
                ItemBackgroundColor1 = new Vector4(0, 0, 0, 0)
            }) as Gui.Widgets.WidgetListView;

            var rightPanel = AddChild(new Widget());

            rightPanel.AddChild(new Gui.Widget
            {
                Text = RightHeader,
                Font = "font16",
                TextColor = new Vector4(0, 0, 0, 1),
                AutoLayout = AutoLayout.DockTop
            });

            MoneyField = rightPanel.AddChild(new MoneyEditor
            {
                MaximumValue = (int)TradeEntity.Money,
                MinimumSize = new Point(0, 33),
                AutoLayout = AutoLayout.DockBottom,
                OnValueChanged = (sender) => Root.SafeCall(OnTotalSelectedChanged, this),
                Tooltip = "Money to trade."
            }) as MoneyEditor;

            var rightList = rightPanel.AddChild(new Gui.Widgets.WidgetListView
            {
                ItemHeight = 32,
                AutoLayout = AutoLayout.DockFill,
                SelectedItemBackgroundColor = new Vector4(0, 0, 0, 0),
                ItemBackgroundColor2 = new Vector4(0, 0, 0, 0.1f),
                ItemBackgroundColor1 = new Vector4(0, 0, 0, 0)
            }) as Gui.Widgets.WidgetListView;

            // Lists should have bidirectional properties.
            SetupList(rightList, leftList, SourceResources, SelectedResources);
            SetupList(leftList, rightList, SelectedResources, SourceResources);
        }

        private void SetupList(WidgetListView listA, WidgetListView listB, List<ResourceAmount> resourcesA, 
            List<ResourceAmount> resourcesB)
        {
            foreach (var resource in resourcesA)
            {
                var lineItem = CreateLineItem(resource);

                var lambdaResource = resource;
                lineItem.TriggerOnChildClick = true;
                lineItem.EnableHoverClick();
                lineItem.OnClick = (sender, args) =>
                {
                    if (lambdaResource.NumResources <= 0) return;
                    var toMove = 1;
                    if (args.Control) toMove = lambdaResource.NumResources;
                    if (args.Shift) toMove = Math.Min(5, lambdaResource.NumResources);
                    if (lambdaResource.NumResources - toMove < 0)
                        return;
                    lambdaResource.NumResources -= toMove;
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_change_selection, 0.1f, MathFunctions.Rand() * 0.25f);
                    var existingEntry = resourcesB.FirstOrDefault(r => r.ResourceType == lambdaResource.ResourceType);
                    if (existingEntry == null)
                    {
                        existingEntry = new ResourceAmount(lambdaResource.ResourceType, 0);
                        resourcesB.Add(existingEntry);
                        var rightLineItem = CreateLineItem(existingEntry);
                        rightLineItem.EnableHoverClick();
                        listA.AddItem(rightLineItem);

                        rightLineItem.TriggerOnChildClick = true;
                        rightLineItem.OnClick = (_sender, _args) =>
                        {
                            var _toMove = 1;
                            if (_args.Control) _toMove = existingEntry.NumResources;
                            if (_args.Shift) _toMove = Math.Min(5, existingEntry.NumResources);
                            if (existingEntry.NumResources - _toMove < 0)
                                return;
                            existingEntry.NumResources -= _toMove;
                            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_change_selection, 0.1f, MathFunctions.Rand() * 0.25f);
                            if (existingEntry.NumResources == 0)
                            {
                                var index = resourcesB.IndexOf(existingEntry);
                                resourcesB.RemoveAt(index);
                                listA.RemoveChild(listA.GetChild(index + 1));
                            }

                            UpdateColumn(listA, resourcesB);

                            var sourceEntry = resourcesA.FirstOrDefault(
                                r => r.ResourceType == existingEntry.ResourceType);
                            sourceEntry.NumResources += _toMove;
                            UpdateLineItemText(
                                listB.GetChild(resourcesA.IndexOf(sourceEntry) + 1),
                                sourceEntry);

                            Root.SafeCall(OnTotalSelectedChanged, this);
                        };
                    }
                    existingEntry.NumResources += toMove;

                    UpdateColumn(listA, resourcesB);
                    UpdateLineItemText(lineItem, lambdaResource);

                    Root.SafeCall(OnTotalSelectedChanged, this);
                };

                listB.AddItem(lineItem);
            }
        }


        private List<ResourceAmount> Clone(List<ResourceAmount> resources)
        {
            List<ResourceAmount> toReturn = new List<ResourceAmount>();
            foreach(var resource in resources)
            {
                toReturn.Add(new ResourceAmount(resource));
            }
            return toReturn;
        }

        public override void Construct()
        {
            SourceResources = Clone(TradeEntity.Resources);

            Reconstruct(SourceResources, SelectedResources, 0);
        }

        private void UpdateColumn(Gui.Widgets.WidgetListView ListView, List<ResourceAmount> selectedResources)
        {
            for (var i = 0; i < SelectedResources.Count; ++i)
                UpdateLineItemText(ListView.GetChild(i + 1), selectedResources[i]);
        }

        private void UpdateRightColumn(Gui.Widgets.WidgetListView ListView)
        {
            UpdateColumn(ListView, SelectedResources);
        }

        private Widget CreateLineItem(ResourceAmount Resource)
        {
            var r = Root.ConstructWidget(new Gui.Widget
            {
                MinimumSize = new Point(1, 32),
                Background = new TileReference("basic", 0)
            });

            var resourceInfo = ResourceLibrary.GetResourceByName(Resource.ResourceType);

            var icon = r.AddChild(new ResourceIcon()
            {
                MinimumSize = new Point(32 + 24, 32 + 16),
                MaximumSize = new Point(32 + 24, 32 + 16),
                Layers = resourceInfo.GuiLayers,
                AutoLayout = AutoLayout.DockLeft,
                BackgroundColor = Resource.NumResources > 0 ? resourceInfo.Tint.ToVector4() : new Vector4(0.5f, 0.5f, 0.5f, 0.5f),
                TextColor = Color.White.ToVector4(),
                TextHorizontalAlign = HorizontalAlign.Right,
                TextVerticalAlign = VerticalAlign.Bottom
            });

            r.AddChild(new Gui.Widget
            {
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(128, 0),
                TextColor = Resource.NumResources > 0 ? Color.Black.ToVector4() : new Vector4(0.5f, 0.5f, 0.5f, 0.5f),
                TextVerticalAlign = VerticalAlign.Center,
                HoverTextColor = GameSettings.Default.Colors.GetColor("Highlight", Color.DarkRed).ToVector4(),
                Font = "font10",
                ChangeColorOnHover = true,
                WrapText = true
            });

            r.AddChild(new Gui.Widget
            {
                AutoLayout = AutoLayout.DockRight,
                //Text = String.Format("{0} at ${1}e", Resource.NumResources, resourceInfo.MoneyValue),
                //Font = "font18-outline",
                //TextColor = new Vector4(1,1,1,1),
                TextColor = Resource.NumResources > 0 ? Color.Black.ToVector4() : new Vector4(0.5f, 0.5f, 0.5f, 0.5f),
                TextVerticalAlign = VerticalAlign.Center,
                HoverTextColor = GameSettings.Default.Colors.GetColor("Highlight", Color.DarkRed).ToVector4(),
                Font = "font10",
                ChangeColorOnHover = true,
            });

            r.Layout();
            UpdateLineItemText(r, Resource);

            return r;
        }

        private void UpdateLineItemText(Widget LineItem, ResourceAmount Resource)
        {
            var resourceInfo = ResourceLibrary.GetResourceByName(Resource.ResourceType);
            var font = LineItem.Root.GetTileSheet("font10") as VariableWidthFont;
            var label = resourceInfo.ShortName ?? resourceInfo.Name; 
            if (font != null)
            {
                Point measurements = font.MeasureString(resourceInfo.ShortName ?? resourceInfo.Name);
                label = font.WordWrapString(label, 1.0f, 128);
                if (128 < measurements.X)
                {
                    LineItem.MinimumSize.Y = font.TileHeight * label.Split('\n').Length;
                }
            }
            LineItem.GetChild(1).Text = label;
            LineItem.GetChild(1).Invalidate();
            LineItem.GetChild(2).Text = String.Format("{0}",
                ValueSourceEntity.ComputeValue(Resource.ResourceType));
            var counter = LineItem.GetChild(0).Children.Last();
            counter.Text = Resource.NumResources.ToString();
            counter.Invalidate();
            LineItem.GetChild(0).Invalidate();
            LineItem.Tooltip = resourceInfo.Name + "\n" + resourceInfo.Description;
            for (int i = 0; i < 3; i++)
            {
                if (i > 0)
                {
                    LineItem.GetChild(i).TextColor = Resource.NumResources > 0
                        ? Color.Black.ToVector4()
                        : new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
                }
                LineItem.GetChild(i).BackgroundColor = Resource.NumResources > 0
                    ? resourceInfo.Tint.ToVector4()
                    : new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
                LineItem.GetChild(i).Tooltip = resourceInfo.Name + "\n" + resourceInfo.Description;
                LineItem.GetChild(i).Invalidate();
            }
        }
    }
}
