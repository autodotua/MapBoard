﻿namespace MapBoard.GeoShare.Core.Entity
{
    public class UserEntity : EntityBase
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public int GroupId { get; set; }
    }
}
