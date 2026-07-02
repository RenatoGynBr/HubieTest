/* =====================================================================
   queueController (AGENT) — TODO (candidate area)
   List the ticket queue (method 'listQueue'), with an optional status filter.
   ===================================================================== */
angular.module('hubieTest').controller('queueController',
    ['$scope', '$state', 'apiService',
    function ($scope, $state, apiService) {

        $scope.tickets = [];
        $scope.statusFilter = '';

        $scope.loading = false;
        $scope.error = null;

        //$scope.load = function () {
        // TODO: apiService.request('ashx/process/ticket.ashx', 'listQueue',
        //       JSON.stringify({ status: $scope.statusFilter })).then(...)
        //};

        $scope.load = function () {
            $scope.loading = true;
            $scope.error = null;

            var statusFilter = $scope.statusFilter ? JSON.stringify({ status: $scope.statusFilter }) : null;

            apiService.request('ashx/process/ticket.ashx', 'listQueue', statusFilter)
                .then(function (r) {
                    $scope.tickets = r.data || [];
                })
                .catch(function (err) {
                    console.error('listQueue error', err);
                    $scope.error = 'Unable to load queue';
                    $scope.tickets = [];
                })
                .finally(function () {
                    $scope.loading = false;
                });
        };

        $scope.handle = function (ticket) {
            $state.go('app.handle', { id: ticket.TICKET_ID });
        };

        $scope.load();
    }
]);
