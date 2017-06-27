using System;
using Android.App;
using Android.OS;
using Android.Widget;

namespace Echo
{
    //calendar dialog
    class EchoDatePicker : DialogFragment, DatePickerDialog.IOnDateSetListener
    {
        private Action<DateTime> _dateSelectedHandler = delegate { };

        public static EchoDatePicker NewInstance(Action<DateTime> onDateSelected)
        {
            return new EchoDatePicker
            {
                _dateSelectedHandler = onDateSelected
            };
        }

        // month is a value between 0 and 11, not 1 and 12!
        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            //default date
            var currently = MainActivity.SelectedDates[MainActivity.CurrentPosition];
            var dialog = new DatePickerDialog(Activity, this, currently.Year, currently.Month - 1, currently.Day);
            //maximum date is today
            dialog.DatePicker.MaxDate = (long)(DateTime.Now.ToUniversalTime() - DateTime.Parse("1/1/1970 0:0:0")).TotalMilliseconds;
            return dialog;
        }

        public void OnDateSet(DatePicker view, int year, int monthOfYear, int dayOfMonth)
        {
            _dateSelectedHandler(new DateTime(year, monthOfYear + 1, dayOfMonth));
        }
    }
}