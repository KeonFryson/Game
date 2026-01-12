using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Kamgam.UVEditor;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;


namespace Kamgam.UVEditor
{
    public partial class UVEditorWindow : EditorWindow
    {
        // UVs 

        private Vector2 _moveDragStartPos;
        private Vector2 _moveDragCumulativeDelta;
        
        private void startMovingUVs(IMouseEvent evt)
        {
            // Clear control to avoid "Should not be capturing when there is a hotcontrol" errors.
            GUI.FocusControl(null);

            // Start undo group, we will end it after the move has stopped.
            UndoStack.Instance.StartGroup();
            UndoStack.Instance.StartEntry();
            UndoStack.Instance.AddUndoAction(undoSetUVsWorkingCopyFunc());
            UndoStack.Instance.EndEntry();

            if (_applyChangesImmediately)
                applyUVChangesToSelectedObjectMesh(recordUndo: true, _uvChannel);

            if (_editTextureEnabled)
                startMovingTexture(evt);
            
            Bounds selectedVertexBounds = getSelectedVerticesBoundsInUVs();
            _moveDragStartPos = selectedVertexBounds.center;
            _moveDragCumulativeDelta = Vector2.zero;
        }

        // Used by the mouse drag.
        private void moveUVs(MouseMoveEvent evt)
        {
            // Update cursor
            _cursorTargetElement.style.cursor = UnityDefaultCursor.DefaultCursor(UnityDefaultCursor.CursorType.MoveArrow);
            _nextCursorOnMouseUp = UnityDefaultCursor.DefaultCursor(UnityDefaultCursor.CursorType.Arrow);

            // Move
            var uvMouseDelta = new Vector2(
                evt.mouseDelta.x / _textureContainer.resolvedStyle.width,
                -evt.mouseDelta.y / _textureContainer.resolvedStyle.height
                );

            _moveDragCumulativeDelta += uvMouseDelta;

            if (!_selectedVertices.IsNullOrEmpty())
            {
                var uvs = getUVWorkingCopyForSelection(_uvChannel);

                foreach (var v in _selectedVertices)
                {
                    if (evt.shiftKey)
                    {
                        var uv = uvs[v];
                        
                        float xGridSteps = (_moveDragStartPos.x + _moveDragCumulativeDelta.x) / _gridSize.x;
                        uv.x = Mathf.RoundToInt(xGridSteps) * _gridSize.x;
                        
                        float yGridSteps = (_moveDragStartPos.y + _moveDragCumulativeDelta.y) / _gridSize.y;
                        uv.y = Mathf.RoundToInt(yGridSteps) * _gridSize.y;
                        
                        uvs[v] = uv;
                    }
                    else
                    {
                        uvs[v] += uvMouseDelta;   
                    }
                }

                // If edit texture is enabled then disable live UV editing.
                if (_applyChangesImmediately && !_editTextureEnabled)
                    applyUVChangesToSelectedObjectMesh(recordUndo: false, _uvChannel);

                // Move Texture
                if (_editTextureEnabled)
                    moveTexture(evt, uvMouseDelta);

                // Update selection rect since the vertices UVs have changed.
                updateSelectionRect();
                updateVertexPosInputs();

                _uvsAreDirty = true;
            }
        }
        
        // Used only by the input fields at the bottom.
        private void moveSelectedU(float u)
        {
            if (!_selectedVertices.IsNullOrEmpty())
            {
                var uvs = getUVWorkingCopyForSelection(_uvChannel);

                foreach (var i in _selectedVertices)
                {
                    var uv = uvs[i];
                    uv.x = u;
                    uvs[i] = uv;
                }

                // If edit texture is enabled then disable live UV editing.
                if (_applyChangesImmediately && !_editTextureEnabled)
                    applyUVChangesToSelectedObjectMesh(recordUndo: false, _uvChannel);
                
                // Update selection rect since the vertices UVs have changed.
                updateSelectionRect();
                
                _uvsAreDirty = true;
            }
        }
        
        // Used only by the input fields at the bottom.
        private void moveSelectedV(float v)
        {
            if (!_selectedVertices.IsNullOrEmpty())
            {
                var uvs = getUVWorkingCopyForSelection(_uvChannel);

                foreach (var i in _selectedVertices)
                {
                    var uv = uvs[i];
                    uv.y = v;
                    uvs[i] = uv;
                }

                // If edit texture is enabled then disable live UV editing.
                if (_applyChangesImmediately && !_editTextureEnabled)
                    applyUVChangesToSelectedObjectMesh(recordUndo: false, _uvChannel);
                
                // Update selection rect since the vertices UVs have changed.
                updateSelectionRect();
                
                _uvsAreDirty = true;
            }
        }

        private void stopMovingUVs()
        {
            UndoStack.Instance.StartEntry();
            UndoStack.Instance.AddRedoAction(undoSetUVsWorkingCopyFunc());
            UndoStack.Instance.EndEntry();

            if (_applyChangesImmediately)
                applyUVChangesToSelectedObjectMesh(recordUndo: true, _uvChannel);

            if (_editTextureEnabled)
                stopMovingTexture();

            UndoStack.Instance.EndGroup();
            _uvsAreDirty = true;
        }
    }
}