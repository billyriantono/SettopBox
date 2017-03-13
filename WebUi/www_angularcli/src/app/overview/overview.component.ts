﻿import { Component, OnInit } from '@angular/core';

import { SettopboxService } from '../settopbox.service';
import { Channel, ChannelLocations } from '../models';

@Component({
    selector: 'app-overview',
    templateUrl: './overview.component.html',
    styleUrls: ['./overview.component.css']
})
export class OverviewComponent implements OnInit {

    channels: Channel[];

    constructor(private settopboxService: SettopboxService) { }

    ngOnInit() {
        this.settopboxService.get().then(response => {
            this.channels = response;
        });
    }
}