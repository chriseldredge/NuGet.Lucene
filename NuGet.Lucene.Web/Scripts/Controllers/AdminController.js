define(['ember'], function (em) {
    return em.ObjectController.extend({
        synchronize: function () {
            App.indexingModel.synchronize();
        },
        cancel: function () {
            App.indexingModel.cancel();
        }
    });
});