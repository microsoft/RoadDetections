// <copyright file="Heap.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Search
{
    using System;

    /// <summary>
    /// Min heap.
    /// </summary>
    /// <typeparam name="T">Type of element of heap.</typeparam>
    public class Heap<T>
        where T : IComparable<T>
    {
        private const int InitialCapacity = 128;
        private const int GrowFactor = 2;
        private T[] heap = new T[InitialCapacity];

        public int Count { get; private set; } = 0;

        public int Capacity { get; private set; } = InitialCapacity;

        public void Add(T item)
        {
            if (this.Count == this.Capacity)
            {
                this.Grow();
            }

            this.heap[this.Count++] = item;
            this.BubbleUp(this.Count - 1);
        }

        public T Top()
        {
            if (this.Count == 0)
            {
                throw new InvalidOperationException("Heap is empty");
            }

            return this.heap[0];
        }

        public void Pop()
        {
            if (this.Count == 0)
            {
                throw new InvalidOperationException("Heap is empty");
            }

            this.Count--;
            this.Swap(this.Count, 0);
            this.BubbleDown(0);
            return;
        }

        private static int Parent(int i)
        {
            return ((i + 1) / 2) - 1;
        }

        private static int YoungChild(int i)
        {
            return ((i + 1) * 2) - 1;
        }

        private static int OldChild(int i)
        {
            return YoungChild(i) + 1;
        }

        private bool Dominates(T x, T y)
        {
            return x.CompareTo(y) <= 0;
        }

        private void BubbleUp(int i)
        {
            if (i == 0 || this.Dominates(this.heap[Parent(i)], this.heap[i]))
            {
                // correct domination (or root)
                return;
            }

            this.Swap(i, Parent(i));
            this.BubbleUp(Parent(i));
        }

        private void BubbleDown(int i)
        {
            int dominatingNode = this.Dominating(i);
            if (dominatingNode == i)
            {
                return;
            }

            this.Swap(i, dominatingNode);
            this.BubbleDown(dominatingNode);
        }

        private int Dominating(int i)
        {
            int dominatingNode = i;
            dominatingNode = this.GetDominating(YoungChild(i), dominatingNode);
            dominatingNode = this.GetDominating(OldChild(i), dominatingNode);

            return dominatingNode;
        }

        private int GetDominating(int newNode, int dominatingNode)
        {
            if (newNode < this.Count && !this.Dominates(this.heap[dominatingNode], this.heap[newNode]))
            {
                return newNode;
            }
            else
            {
                return dominatingNode;
            }
        }

        private void Swap(int i, int j)
        {
            (this.heap[j], this.heap[i]) = (this.heap[i], this.heap[j]);
        }

        private void Grow()
        {
            int newCapacity = this.Capacity * GrowFactor;
            var newHeap = new T[newCapacity];
            Array.Copy(this.heap, newHeap, this.Capacity);
            this.heap = newHeap;
            this.Capacity = newCapacity;
        }
    }
}
