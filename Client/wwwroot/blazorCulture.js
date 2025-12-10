window.blazorCulture = {
  get: function () {
    try {
      return window.localStorage.getItem('blazor-culture');
    } catch (e) { return null; }
  },
  set: function (value) {
    try {
      window.localStorage.setItem('blazor-culture', value);
    } catch (e) { }
  }
};