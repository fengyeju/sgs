﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Controls;

using Sanguosha.Core.Players;
using Sanguosha.Core.Heroes;
using Sanguosha.Core.Skills;
using Sanguosha.Core.Games;
using Sanguosha.Core.Cards;

namespace Sanguosha.UI.Controls
{
    public class NumRolesToComboBoxEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            List<Role> roles = value as List<Role>;
            return (roles != null) && (roles.Count > 1);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class PlayerInfoViewModel : ViewModelBase
    {
        #region Constructors
        public PlayerInfoViewModel()
        {
        }

        public PlayerInfoViewModel(Player p)
        {
            Player = p;        
        }
        #endregion

        #region Fields
        Player _player;

        public Player Player
        {
            get { return _player; }
            set 
            {
                if (_player == value) return;
                if (_player != null)
                {
                    PropertyChangedEventHandler handler = _PropertyChanged;
                    _player.PropertyChanged -= handler;
                }
                _player = value;
                _PropertyChanged = _OnPlayerPropertyChanged;                
                _player.PropertyChanged += _PropertyChanged;
                var properties = typeof(Player).GetProperties();
                foreach (var property in properties)
                {
                    _OnPlayerPropertyChanged(_player, new PropertyChangedEventArgs(property.Name));
                }
            }
        }

        private Game _game;

        public Game Game 
        {
            get { return _game; }
            set
            {
                if (_game == value) return;
                bool changed = (_game == null || _game.GetType() != value.GetType());                
                _game = value;
                _game.PropertyChanged += new PropertyChangedEventHandler(_game_PropertyChanged);
                if (changed)
                {
                    OnPropertyChanged("PossibleRoles");
                }
            }
        }

        void _game_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            string name = e.PropertyName;
            if (name == "CurrentPlayer")
            {   
                OnPropertyChanged("IsCurrentPlayer");
            }
            else if (name == "CurrentPhase")
            {                
                OnPropertyChanged("CurrentPhase");
            }
        }

        private PropertyChangedEventHandler _PropertyChanged;

        private void _OnPlayerPropertyChanged(object o, PropertyChangedEventArgs e)
        {
            string name = e.PropertyName;
            if (name == "Role")
            {
                OnPropertyChanged("PossibleRoles");
            }
            else if (name == "Hero")
            {
                OnPropertyChanged("HeroName");
            }
            else
            {
                var propNames = from prop in this.GetType().GetProperties() select prop.Name;
                if (propNames.Contains(name))
                {
                    OnPropertyChanged(name);
                }
            }    
        }

        #endregion

        #region Player Properties
        public Allegiance Allegiance
        {
            get
            {
                if (_player == null)
                {
                    return Allegiance.Unknown;
                }
                else
                {
                    return _player.Allegiance;
                }
            }
        }

        public int Health
        {
            get
            {
                if (_player == null)
                {
                    return 0;
                }
                else
                {
                    return _player.Health;
                }
            }
        }

        public int MaxHealth
        {
            get
            {
                if (_player == null)
                {
                    return 0;
                }
                else
                {
                    return _player.MaxHealth;
                }
            }
        }

        public Hero Hero
        {
            get
            {
                return _player.Hero;
            }
        }

        public Hero Hero2
        {
            get
            {
                return _player.Hero2;
            }
        }

        public int Id
        {
            get
            {
                return _player.Id;
            }
        }

        public bool IsFemale
        {
            get
            {
                return _player.IsFemale;
            }
        }

        public bool IsMale
        {
            get
            {
                return _player.IsMale;
            }
        }        

        public List<ISkill> Skills
        {
            get 
            {
                return _player.Skills; 
            }
        }

        public bool IsTargeted { get { return _player.IsTargeted; } }

        public bool IsCurrentPlayer 
        {
            get 
            {
                if (_game == null) return false;
                else
                {
                    return _player == _game.CurrentPlayer;
                }
            } 
        }

        public TurnPhase CurrentPhase
        {
            get
            {
                if (!IsCurrentPlayer)
                {
                    return TurnPhase.Inactive;
                }
                return _game.CurrentPhase;
            }
        }

        #endregion
        
        #region Derived Player Properties

        public string HeroName
        {
            get
            {
                if (_player == null || _player.Hero == null)
                {
                    return string.Empty;
                }
                else
                {
                    return (_player.Hero.Name);
                }
            }
        }

        private static List<Role> roleGameRoles = new List<Role>() { Role.Loyalist, Role.Defector, Role.Rebel };

        public List<Role> PossibleRoles
        {
            get
            {
                List<Role> roles = new List<Role>();
                roles.Add(Role.Unknown);
                if (Game != null)
                {
                    if (Game is RoleGame)
                    {
                        if (_player.Role == Role.Unknown)
                        {
                            roles.AddRange(roleGameRoles);
                        }
                        else if (_player.Role == Role.Ruler)
                        {
                            roles.Clear();
                            roles.Add(_player.Role);
                        }
                        else
                        {
                            roles.Add(_player.Role);
                        }
                    }
                    else
                    {
                        // @todo: add other possibilities here.
                    }
                }
                return roles;
            }
        }

        #endregion

        #region UI Related Properties
                
        private bool isSelected;

        public bool IsSelected
        {
            get { return isSelected; }
            set { isSelected = value; OnPropertyChanged("IsSelected"); }
        }        

        #endregion
    }
}