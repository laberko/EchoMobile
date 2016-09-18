using System;
using Android.App;
using Android.OS;
using Android.Widget;

namespace Echo
{
    class DatePickerFragment : DialogFragment, DatePickerDialog.IOnDateSetListener
    {
        Action<DateTime> _dateSelectedHandler = delegate { };
        // Initialize this value to prevent NullReferenceExceptions
        public static readonly string TAG = typeof(DatePickerFragment).Name;

        public static DatePickerFragment NewInstance(Action<DateTime> onDateSelected)
        {
            return new DatePickerFragment { _dateSelectedHandler = onDateSelected };
        }

        // month is a value between 0 and 11, not 1 and 12!
        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            var currently = DateTime.Now.ToUniversalTime();
            var dialog = new DatePickerDialog(Activity, this, currently.Year, currently.Month - 1, currently.Day);
            dialog.DatePicker.MaxDate = (long)(currently - DateTime.Parse("1/1/1970 0:0:0")).TotalMilliseconds;
            return dialog;
        }

        public void OnDateSet(DatePicker view, int year, int monthOfYear, int dayOfMonth)
        {
            _dateSelectedHandler(new DateTime(year, monthOfYear + 1, dayOfMonth));
        }
    }
}