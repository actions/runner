import React from 'react';
import ReactDOM from 'react-dom';
import { MemoryRouter } from 'react-router-dom';
import { mount, shallow } from 'enzyme';
import { ListItemLink, ListItemLinkProps } from './ListItemLink';
import { ListItem } from '../ListItem/ListItem';

describe('<ListItemLink />', () => {

    let div: any;
    let props: ListItemLinkProps;

    beforeEach(() => {
        div = document.createElement('div');
        props = {
            item: {
                id: 0,
                title: 'Test Title',
                description: 'Test Description'
            },
            to: '/detail/1'
        };
    });

    it('[SMOKE - DEEP] renders without crashing', () => {
        ReactDOM.render(
            <MemoryRouter>
                <ListItemLink {...props} />
            </MemoryRouter>, div);
        ReactDOM.unmountComponentAtNode(div);
    });

    it('[SMOKE - SHALLOW] renders without crashing', () => {
        shallow(
            <MemoryRouter>
                <ListItemLink {...props} />
            </MemoryRouter>);
    });

    it('The correct props are passed to the child component', () => {
        const wrapper = mount(
            <MemoryRouter>
                <ListItemLink {...props} />
            </MemoryRouter>);
        const child = wrapper.find(ListItem); 
        expect(child.length).toEqual(1);
        expect(child.props()).toEqual(props);
    });

});