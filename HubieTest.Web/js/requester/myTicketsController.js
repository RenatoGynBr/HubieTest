/* =====================================================================
   myTicketsController (REQUESTER) — TODO (candidate area)
   List the logged-in requester's tickets (method 'listMine').
   ===================================================================== */
angular.module('hubieTest').controller('myTicketsController',
    ['$scope', '$state', 'apiService',
    function ($scope, $state, apiService) {

        $scope.tickets = [];
        $scope.loading = false;
        $scope.error = null;

        $scope.load = function () {
            $scope.loading = true;
            $scope.error = null;

            apiService.request('ashx/process/ticket.ashx', 'listMine', null)
                .then(function (r) {
                    $scope.tickets = r.data || [];
                })
                .catch(function (err) {
                    console.error('listMine error', err);
                    $scope.error = 'Unable to load your tickets';
                    $scope.tickets = [];
                })
                .finally(function () {
                    $scope.loading = false;
                });
        };

        $scope.openDetail = function (ticket) {
            $state.go('app.ticketDetail', { id: ticket.TICKET_ID });
        };

        $scope.load();
    }
]);
