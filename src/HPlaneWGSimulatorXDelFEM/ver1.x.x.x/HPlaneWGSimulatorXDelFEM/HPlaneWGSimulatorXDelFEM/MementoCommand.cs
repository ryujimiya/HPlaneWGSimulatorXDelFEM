using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyUtilLib
{
    /// <summary>
    /// 思い出更新コマンド
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class MementoCommand<T1, T2> : ICommand, IDisposable
    {
        private Memento<T1, T2> _memento;
        private T1 _prev;
        private T1 _next;

        public MementoCommand(Memento<T1, T2> prev, Memento<T1, T2> next)
        {
            //System.Diagnostics.Debug.WriteLine("MementoCommand Constructor");
            _memento = prev;
            //  Note: 受け取ったMementoの破棄責任はMementoCommandにある
            _prev = prev.MementoData;
            _next = next.MementoData;
            //  Note: getしたインスタンスはコピーなので破棄の責任はMementoCommand側にある
            //System.Diagnostics.Debug.WriteLine("  MementoCommand Constructor done");
        }

        #region ICommand メンバ

        /// <summary>
        /// 呼び出し
        /// </summary>
        void ICommand.Invoke()
        {
            //System.Diagnostics.Debug.WriteLine("MementoCommand Invoke");
            if (_prev != null && _prev is IDisposable)
            {
                ((IDisposable)_prev).Dispose();
                _prev = default(T1);
            }
            _prev = _memento.MementoData;
            //  Note: getしたインスタンスはコピーなので破棄の責任はMementoCommand側にある
            _memento.SetMemento(_next);
            //System.Diagnostics.Debug.WriteLine("  MementoCommand Invoke done");
        }

        /// <summary>
        /// 元に戻す
        /// </summary>
        void ICommand.Undo()
        {
            //System.Diagnostics.Debug.WriteLine("MementoCommand Undo");
            _memento.SetMemento(_prev);
            //System.Diagnostics.Debug.WriteLine("  MementoCommand Undo done");
        }

        /// <summary>
        /// やり直し
        /// </summary>
        void ICommand.Redo()
        {
            //System.Diagnostics.Debug.WriteLine("MementoCommand Redo");
            _memento.SetMemento(_next);
            //System.Diagnostics.Debug.WriteLine("  MementoCommand Redo done");
        }

        /// <summary>
        /// デストラクタ
        /// </summary>
        ~MementoCommand()
        {
            //System.Diagnostics.Debug.WriteLine("MementoCommand Finalizer");
            Dispose(false);
            //System.Diagnostics.Debug.WriteLine("  MementoCommand Finalizer done");
        }

        /// <summary>
        /// 破棄
        /// </summary>
        /// <param name="dispoing"></param>
        private void Dispose(bool disposing)
        {
            //System.Diagnostics.Debug.WriteLine("MementoCommand Dispose {0}", disposing);
            if (_memento != null && _memento is IDisposable)
            {
                ((IDisposable)_memento).Dispose();
                _memento = null;
            }
            if (_prev != null && _prev is IDisposable)
            {
                ((IDisposable)_prev).Dispose();
                _prev = default(T1);
            }
            if (_next != null && _next is IDisposable)
            {
                ((IDisposable)_next).Dispose();
                _next = default(T1);
            }
            //System.Diagnostics.Debug.WriteLine("  MementoCommand Dispose {0} done", disposing);
        }

        /// <summary>
        /// 破棄
        /// </summary>
        void IDisposable.Dispose()
        {
            //System.Diagnostics.Debug.WriteLine("MementoCommand Dispose");
            Dispose(true);
            GC.SuppressFinalize(this);
            //System.Diagnostics.Debug.WriteLine("  MementoCommand Dispose done");
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            ((IDisposable)this).Dispose();
        }

        #endregion
    }
}
