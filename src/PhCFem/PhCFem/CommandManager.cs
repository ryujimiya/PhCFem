using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyUtilLib
{
    /// <summary>
    /// CommandManager
    /// </summary>
    public sealed class CommandManager
    {
        private int _maxStack = int.MaxValue;
        private Stack<ICommand> _undoStack;
        private Stack<ICommand> _redoStack;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public CommandManager()
        {
            _undoStack = new Stack<ICommand>();
            _redoStack = new Stack<ICommand>();
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="max">最大保存数</param>
        public CommandManager(int maxStack)
            : this()
        {
            _maxStack = maxStack;
        }

        /// <summary>
        /// 呼び出し
        /// </summary>
        /// <param name="command">コマンド</param>
        public bool Invoke(ICommand command)
        {
            if (_undoStack.Count >= _maxStack) return false;
            command.Invoke();
            _redoStack.Clear();
            _undoStack.Push(command);
            return true;
        }

        /// <summary>
        /// 元に戻す
        /// </summary>
        public void Undo()
        {
            if (_undoStack.Count == 0) return;
            var command = _undoStack.Pop();
            command.Undo();
            _redoStack.Push(command);
        }

        /// <summary>
        /// やり直し
        /// </summary>
        public void Redo()
        {
            if (_redoStack.Count == 0) return;
            var command = _redoStack.Pop();
            command.Redo();
            _undoStack.Push(command);
        }

        /// <summary>
        /// リフレッシュ
        /// </summary>
        public void Refresh()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }

        /// <summary>
        /// 元に戻す操作が可能か
        /// </summary>
        /// <returns></returns>
        public bool CanUndo()
        {
            return _undoStack.Count > 0;
        }

        /// <summary>
        /// やり直し操作が可能か
        /// </summary>
        /// <returns></returns>
        public bool CanRedo()
        {
            return _redoStack.Count > 0;
        }
    }
}
