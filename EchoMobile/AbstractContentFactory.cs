using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Echo
{
    public abstract class AbstractContentFactory
    {
        private List<AbstractContent> _items;
        public DateTime ContentDate;
        public int NewItemsCount;

        protected AbstractContentFactory(DateTime day)
        {
            _items = new List<AbstractContent>();
            ContentDate = day;
        }

        //items collection
        public List<AbstractContent> ContentList
        {
            get
            {
                return _items;
            }
            protected set
            {
                _items = value;
                NotifyPropertyChanged();
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged()
        {
            //raise PropertyChanged event and pass changed property name
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(GetType().Name));

        }

        public abstract void GetContent();

        //indexer (read only) for accessing an item
        public AbstractContent this[int i] => ContentList.Count == 0 ? null : ContentList[i];

    }
}