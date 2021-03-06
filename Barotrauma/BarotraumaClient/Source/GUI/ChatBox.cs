﻿using Barotrauma.Items.Components;
using Barotrauma.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Barotrauma
{
    class ChatBox
    {
        private static Sprite radioIcon;
        private GUIListBox chatBox;
        private Point screenResolution;

        public bool IsSinglePlayer { get; private set; }

        private bool _toggleOpen = true;
        public bool ToggleOpen
        {
            get { return _toggleOpen; }
            set
            {
                if (_toggleOpen == value) { return; }
                _toggleOpen = GameMain.Config.ChatOpen = value;
                foreach (GUIComponent child in ToggleButton.Children)
                {
                    child.SpriteEffects = _toggleOpen == (HUDLayoutSettings.ChatBoxAlignment == Alignment.Right) ?
                      SpriteEffects.FlipHorizontally : SpriteEffects.None;
                }
            }
        }
        private float openState;

        private float prevUIScale;

        //individual message texts that pop up when the chatbox is hidden
        const float PopupMessageDuration = 5.0f;
        private float popupMessageTimer;
        private Queue<GUIComponent> popupMessages = new Queue<GUIComponent>();

        public GUITextBox.OnEnterHandler OnEnterMessage
        {
            get { return InputBox.OnEnterPressed; }
            set { InputBox.OnEnterPressed = value; }
        }

        public GUIFrame GUIFrame { get; private set; }

        public GUITextBox InputBox { get; private set; }

        public GUIButton ToggleButton { get; private set; }

        public ChatBox(GUIComponent parent, bool isSinglePlayer)
        {
            this.IsSinglePlayer = isSinglePlayer;
            if (radioIcon == null)
            {
                radioIcon = new Sprite("Content/UI/inventoryAtlas.png", new Rectangle(527, 952, 38, 52), null);
                radioIcon.Origin = radioIcon.size / 2;
            }

            screenResolution = new Point(GameMain.GraphicsWidth, GameMain.GraphicsHeight);

            int toggleButtonWidth = (int)(30 * GUI.Scale);
            GUIFrame = new GUIFrame(HUDLayoutSettings.ToRectTransform(HUDLayoutSettings.ChatBoxArea, parent.RectTransform), style: null);
            var chatBoxHolder = new GUIFrame(new RectTransform(new Vector2(1.0f, 0.9f), GUIFrame.RectTransform), style: "ChatBox");
            chatBox = new GUIListBox(new RectTransform(new Vector2(1.0f, 0.95f), chatBoxHolder.RectTransform, Anchor.CenterRight), style: null);
            ToggleButton = new GUIButton(new RectTransform(new Point(toggleButtonWidth, HUDLayoutSettings.ChatBoxArea.Height), parent.RectTransform),
                style: "UIToggleButton");

            ToggleButton.OnClicked += (GUIButton btn, object userdata) =>
            {
                ToggleOpen = !ToggleOpen;
                return true;
            };

            InputBox = new GUITextBox(new RectTransform(new Vector2(1.0f, 0.1f), GUIFrame.RectTransform, Anchor.BottomCenter),
                style: "ChatTextBox")
            {
                Font = GUI.SmallFont,
                MaxTextLength = ChatMessage.MaxLength
            };
            InputBox.OnDeselected += (gui, Keys) =>
            {
                gui.Text = "";
            };
            
            ToggleOpen = GameMain.Config.ChatOpen;
        }

        public bool TypingChatMessage(GUITextBox textBox, string text)
        {
            string command = ChatMessage.GetChatMessageCommand(text, out _);
            if (IsSinglePlayer)
            {
                //radio is the only allowed special message type in single player
                if (command != "r" && command != "radio")
                {
                    command = "";
                }
            }

            switch (command)
            {
                case "r":
                case "radio":
                    textBox.TextColor = ChatMessage.MessageColor[(int)ChatMessageType.Radio];
                    break;
                case "d":
                case "dead":
                    textBox.TextColor = ChatMessage.MessageColor[(int)ChatMessageType.Dead];
                    break;
                default:
                    if (Character.Controlled != null && (Character.Controlled.IsDead || Character.Controlled.SpeechImpediment >= 100.0f))
                    {
                        textBox.TextColor = ChatMessage.MessageColor[(int)ChatMessageType.Dead];
                    }
                    else if (command != "") //PMing
                    {
                        textBox.TextColor = ChatMessage.MessageColor[(int)ChatMessageType.Private];
                    }
                    else
                    {
                        textBox.TextColor = ChatMessage.MessageColor[(int)ChatMessageType.Default];
                    }
                    break;
            }

            return true;
        }

        public void AddMessage(ChatMessage message)
        {
            while (chatBox.Content.CountChildren > 20)
            {
                chatBox.RemoveChild(chatBox.Content.Children.First());
            }

            float prevSize = chatBox.BarSize;

            string displayedText = message.TranslatedText;
            string senderName = "";
            Color senderColor = Color.White;
            if (!string.IsNullOrWhiteSpace(message.SenderName))
            {
                senderName = (message.Type == ChatMessageType.Private ? "[PM] " : "") + message.SenderName;
            }
            if (message.Sender?.Info?.Job != null)
            {
                senderColor = Color.Lerp(message.Sender.Info.Job.Prefab.UIColor, Color.White, 0.25f);
            }

            var msgHolder = new GUIFrame(new RectTransform(new Vector2(0.95f, 0.0f), chatBox.Content.RectTransform, Anchor.TopCenter), style: null,
                    color: ((chatBox.Content.CountChildren % 2) == 0) ? Color.Transparent : Color.Black * 0.1f);

            GUITextBlock senderNameBlock = null;
            if (!string.IsNullOrEmpty(senderName))
            {
                senderNameBlock = new GUITextBlock(new RectTransform(new Vector2(0.98f, 0.0f), msgHolder.RectTransform)
                { AbsoluteOffset = new Point((int)(5 * GUI.Scale), 0) },
                    senderName, textColor: senderColor, font: GUI.SmallFont, textAlignment: Alignment.TopLeft, style: null)
                {
                    CanBeFocused = true
                };
            }

            new GUITextBlock(new RectTransform(new Vector2(1.0f, 0.0f), msgHolder.RectTransform)
            { AbsoluteOffset = new Point((int)(10 * GUI.Scale), senderNameBlock == null ? 0 : senderNameBlock.Rect.Height) },
                displayedText, textColor: message.Color, font: GUI.SmallFont, textAlignment: Alignment.TopLeft, style: null, wrap: true,
                color: ((chatBox.Content.CountChildren % 2) == 0) ? Color.Transparent : Color.Black * 0.1f)
            {
                UserData = message.SenderName,
                CanBeFocused = true
            };

            if (message is OrderChatMessage orderChatMsg &&
                Character.Controlled != null &&
                orderChatMsg.TargetCharacter == Character.Controlled)
            {
                msgHolder.Flash(Color.OrangeRed * 0.6f, flashDuration: 5.0f);
            }
            else
            {
                msgHolder.Flash(Color.Yellow * 0.6f);
            }
            //resize the holder to match the size of the message and add some spacing
            msgHolder.RectTransform.Resize(new Point(msgHolder.Rect.Width, msgHolder.Children.Sum(c => c.Rect.Height) + (int)(10 * GUI.Scale)), resizeChildren: false);

            CoroutineManager.StartCoroutine(UpdateMessageAnimation(msgHolder, 0.5f));

            chatBox.UpdateScrollBarSize();

            if (!ToggleOpen)
            {
                var popupMsg = new GUIFrame(new RectTransform(Vector2.One, GUIFrame.RectTransform), style: "GUIToolTip")
                {
                    Visible = false
                };
                var senderText = new GUITextBlock(new RectTransform(new Vector2(1.0f, 0.0f), popupMsg.RectTransform, Anchor.TopRight),
                    senderName, textColor: senderColor, font: GUI.SmallFont, textAlignment: Alignment.TopRight)
                {
                    CanBeFocused = false
                };
                var msgText = new GUITextBlock(new RectTransform(new Vector2(0.8f, 0.0f), popupMsg.RectTransform, Anchor.TopRight)
                    { AbsoluteOffset = new Point(0, senderText.Rect.Height) },
                    displayedText, textColor: message.Color, font: GUI.SmallFont, textAlignment: Alignment.TopRight, style: null, wrap: true)
                {
                    CanBeFocused = false
                };
                int textWidth = (int)Math.Max(
                    msgText.Font.MeasureString(msgText.WrappedText).X,
                    senderText.Font.MeasureString(senderText.WrappedText).X);
                popupMsg.RectTransform.Resize(new Point(textWidth + 20, msgText.Rect.Bottom - senderText.Rect.Y), resizeChildren: false);
                popupMessages.Enqueue(popupMsg);
            }

            if ((prevSize == 1.0f && chatBox.BarScroll == 0.0f) || (prevSize < 1.0f && chatBox.BarScroll == 1.0f)) chatBox.BarScroll = 1.0f;

            GUISoundType soundType = GUISoundType.ChatMessage;
            if (message.Type == ChatMessageType.Radio)
            {
                soundType = GUISoundType.RadioMessage;
            }
            else if (message.Type == ChatMessageType.Dead)
            {
                soundType = GUISoundType.DeadMessage;
            }

            GUI.PlayUISound(soundType);
        }

        private IEnumerable<object> UpdateMessageAnimation(GUIComponent message, float animDuration)
        {
            float timer = 0.0f;
            while (timer < animDuration)
            {
                timer += CoroutineManager.DeltaTime;
                float wavePhase = timer / animDuration * MathHelper.TwoPi;
                message.RectTransform.ScreenSpaceOffset =
                    new Point((int)(Math.Sin(wavePhase) * (1.0f - timer / animDuration) * 50.0f), 0);
                yield return CoroutineStatus.Running;
            }
            message.RectTransform.ScreenSpaceOffset = Point.Zero;
            yield return CoroutineStatus.Success;
        }

        private void SetUILayout()
        {
            GUIFrame.RectTransform.AbsoluteOffset = Point.Zero;
            GUIFrame.RectTransform.RelativeOffset = new Vector2(
                HUDLayoutSettings.ChatBoxArea.X / (float)GameMain.GraphicsWidth,
                HUDLayoutSettings.ChatBoxArea.Y / (float)GameMain.GraphicsHeight);
            GUIFrame.RectTransform.NonScaledSize = HUDLayoutSettings.ChatBoxArea.Size;

            int toggleButtonWidth = (int)(30 * GUI.Scale);
            //make room for the toggle button
            if (HUDLayoutSettings.ChatBoxAlignment == Alignment.Left)
            {
                GUIFrame.RectTransform.AbsoluteOffset += new Point(toggleButtonWidth, 0);
            }
            GUIFrame.RectTransform.NonScaledSize -= new Point(toggleButtonWidth, 0);

            ToggleButton.RectTransform.NonScaledSize = new Point(toggleButtonWidth, HUDLayoutSettings.ChatBoxArea.Height);
            ToggleButton.RectTransform.AbsoluteOffset = HUDLayoutSettings.ChatBoxAlignment == Alignment.Left ?
                new Point(HUDLayoutSettings.ChatBoxArea.X, HUDLayoutSettings.ChatBoxArea.Y) :
                new Point(HUDLayoutSettings.ChatBoxArea.Right - toggleButtonWidth, HUDLayoutSettings.ChatBoxArea.Y);
        }

        public void Update(float deltaTime)
        {
            if (GameMain.GraphicsWidth != screenResolution.X || GameMain.GraphicsHeight != screenResolution.Y || prevUIScale != GUI.Scale)
            {
                SetUILayout();
                screenResolution = new Point(GameMain.GraphicsWidth, GameMain.GraphicsHeight);
                prevUIScale = GUI.Scale;
            }

            if (ToggleOpen || (InputBox != null && InputBox.Selected))
            {
                openState += deltaTime * 5.0f;
                //delete all popup messages when the chatbox is open
                while (popupMessages.Count > 0)
                {
                    var popupMsg = popupMessages.Dequeue();
                    popupMsg.Parent.RemoveChild(popupMsg);
                }
            }
            else
            {
                openState -= deltaTime * 5.0f;

                //make the first popup message visible
                var popupMsg = popupMessages.Count > 0 ? popupMessages.Peek() : null;
                if (popupMsg != null)
                {
                    int offset = -popupMsg.Rect.Width - ToggleButton.Rect.Width * 2 - (int)(50 * GUI.Scale) - (GUIFrame.Rect.X - GameMain.GraphicsWidth);
                    popupMsg.Visible = true;
                    //popup messages appear and disappear faster when there's more pending messages
                    popupMessageTimer += deltaTime * popupMessages.Count * popupMessages.Count;
                    if (popupMessageTimer > PopupMessageDuration)
                    {
                        //move the message out of the screen and delete it
                        popupMsg.RectTransform.ScreenSpaceOffset =
                            new Point((int)MathHelper.SmoothStep(offset, 10, (popupMessageTimer - PopupMessageDuration) * 5.0f), 0);
                        if (popupMessageTimer > PopupMessageDuration + 1.0f)
                        {
                            popupMessageTimer = 0.0f;
                            popupMsg.Parent.RemoveChild(popupMsg);
                            popupMessages.Dequeue();
                        }
                    }
                    else
                    {
                        //move the message on the screen
                        popupMsg.RectTransform.ScreenSpaceOffset = new Point(
                            (int)MathHelper.SmoothStep(0, offset, popupMessageTimer * 5.0f), 0);
                    }
                }
            }
            openState = MathHelper.Clamp(openState, 0.0f, 1.0f);
            int hiddenBoxOffset = GUIFrame.Rect.Width + ToggleButton.Rect.Width;
            GUIFrame.RectTransform.AbsoluteOffset =
                new Point((int)MathHelper.SmoothStep(hiddenBoxOffset * (HUDLayoutSettings.ChatBoxAlignment == Alignment.Left ? -1 : 1), 0, openState), 0);
        }
    }
}
