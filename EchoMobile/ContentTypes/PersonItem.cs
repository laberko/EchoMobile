using System;
using System.Collections.Generic;

namespace Echo.ContentTypes
{
    public class PersonItem
    {
        public Guid PersonId;
        public string PersonName;
        public string PersonUrl;
        public string PersonPhotoUrl;
        public string PersonAbout;

        public static List<PersonItem> AddDummies()
        {
            var list = new List<PersonItem>
            {
                new PersonItem
                {
                    PersonId = Guid.NewGuid(),
                    PersonName = "���� ��������",
                    PersonPhotoUrl = "http://echo.msk.ru/files/avatar2/783858.jpg"
                },
                new PersonItem
                {
                    PersonId = Guid.NewGuid(),
                    PersonName = "������ ����������",
                    PersonPhotoUrl = "http://echo.msk.ru/files/avatar2/1350821.jpg"
                },
                new PersonItem
                {
                    PersonId = Guid.NewGuid(),
                    PersonName = "������� ����������",
                    PersonPhotoUrl = "http://echo.msk.ru/files/avatar2/680010.jpg"
                },
                new PersonItem
                {
                    PersonId = Guid.NewGuid(),
                    PersonName = "������� ������",
                    PersonPhotoUrl = "http://echo.msk.ru/files/avatar2/2429344.jpg"
                },
                new PersonItem
                {
                    PersonId = Guid.NewGuid(),
                    PersonName = "��������� ������",
                    PersonPhotoUrl = "http://echo.msk.ru/files/avatar2/684261.gif"
                },
                new PersonItem
                {
                    PersonId = Guid.NewGuid(),
                    PersonName = "������ ������������",
                    PersonPhotoUrl = "http://echo.msk.ru/files/avatar2/815830.jpg"
                },
                new PersonItem
                {
                    PersonId = Guid.NewGuid(),
                    PersonName = "������ ����������",
                    PersonPhotoUrl = "http://echo.msk.ru/files/avatar2/737220.jpg"
                },
                new PersonItem
                {
                    PersonId = Guid.NewGuid(),
                    PersonName = "������� ������������",
                    PersonPhotoUrl = "http://echo.msk.ru/files/avatar2/804601.jpg"
                }
            };
            return list;
        }
    }
}